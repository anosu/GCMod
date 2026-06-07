using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GCMod.Interfaces;
using Utility.Toast;

namespace GCMod
{
    /// <summary>
    /// Manifest-driven translation cache with local persistence.
    ///
    /// Usage:
    ///   await cache.LoadAsync("names");
    ///   await cache.LoadAsync("novels", "10005");
    ///
    /// Load flow:
    ///   1. Check if resource hash exists in manifest
    ///   2. If local cache file exists, compute its canonical hash
    ///   3. If hash matches manifest → use local cache
    ///   4. Otherwise → fetch from remote and save to local cache
    /// </summary>
    public class TranslationCache
    {
        private readonly string _cdn;
        private readonly string _cacheDir;
        private readonly string _language;
        private readonly HttpClient _client;
        private ManifestData _manifest;

        /// <summary>Prevents concurrent loads of the same resource.</summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        /// <summary>Locks collection size is bounded — cleanup unreferenced entries periodically.</summary>
        private int _lockCleanupCounter;

        private const int LockCleanupInterval = 32;

        private static readonly JsonSerializerOptions HashJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
        };

        /// <summary>UTF-8 without BOM, used for all file I/O.</summary>
        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        public TranslationCache(string cdn, string cacheDir, string language, HttpClient client)
        {
            _cdn = cdn.TrimEnd('/');
            _cacheDir = cacheDir;
            _language = language;
            _client = client;

            var langDir = Path.Combine(_cacheDir, _language);
            Directory.CreateDirectory(langDir);
            Directory.CreateDirectory(Path.Combine(langDir, "novels"));
        }

        public ManifestData Manifest => _manifest;

        /// <summary>
        /// Fetch the remote manifest and cache it locally.
        /// Falls back to local cached manifest on failure.
        /// </summary>
        public async Task FetchManifestAsync()
        {
            var url = BuildRemoteUrl("manifest");
            var path = BuildCachePath("manifest");
            try
            {
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _manifest = JsonSerializer.Deserialize<ManifestData>(json);
                    if (_manifest == null)
                    {
                        ModLogger.Warn("Remote manifest parse returned null");
                    }
                    else
                    {
                        await File.WriteAllTextAsync(path, json, Utf8);
                        ModLogger.Info($"Manifest loaded ({_language}). Hash: {_manifest.Hash}");
                        return;
                    }
                }
                ModLogger.Warn($"Manifest fetch returned {response.StatusCode}");
            }
            catch (Exception e)
            {
                ModLogger.Error($"Failed to fetch manifest: {e.Message}");
            }

            // Fallback: try loading locally cached manifest
            TryLoadLocalManifest(path);
        }

        /// <summary>
        /// Try to load a previously cached manifest from disk.
        /// </summary>
        private void TryLoadLocalManifest(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path, Utf8);
                    _manifest = JsonSerializer.Deserialize<ManifestData>(json);
                    if (_manifest != null)
                    {
                        ModLogger.Info($"Loaded cached manifest from local ({_language}). Hash: {_manifest.Hash}");
                        Toast.Warn("翻译服务", "无法连接远程，使用本地翻译清单");
                    }
                    else
                    {
                        ModLogger.Warn("Cached manifest parse returned null");
                    }
                }
                catch (Exception e)
                {
                    ModLogger.Error($"Failed to load local manifest: {e.Message}");
                }
            }
            else
            {
                ModLogger.Warn("No local manifest cache available, will fetch without hash verification.");
                Toast.Warn("翻译服务", "翻译清单不可用，将直接请求翻译");
            }
        }

        /// <summary>
        /// Load translation data with cache-aware logic.
        /// </summary>
        /// <param name="type">Translation type: "names", "words", or "novels"</param>
        /// <param name="id">Optional identifier (e.g., novelId for novels)</param>
        /// <returns>The loaded dictionary, or null on failure</returns>
        public async Task<Dictionary<string, string>> LoadAsync(string type, string id = null)
        {
            string cacheKey = id != null ? $"{_language}/{type}/{id}" : $"{_language}/{type}";
            string remoteUrl = BuildRemoteUrl(type, id);
            string cachePath = BuildCachePath(type, id);
            string expectedHash = GetManifestHash(type, id);

            // Serialize concurrent loads of the same resource
            var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();
            try
            {
                // If manifest has an expected hash for this resource, check local cache first
                if (expectedHash != null)
                {
                    if (File.Exists(cachePath))
                    {
                        string localHash = HashFile(cachePath);
                        if (localHash == expectedHash)
                        {
                            ModLogger.Info($"Cache hit: {cacheKey}");
                            return LoadFromFile(cachePath);
                        }
                        ModLogger.Info(
                            $"Cache hash mismatch for {cacheKey}, " +
                            $"expected={expectedHash}, local={localHash}"
                        );
                    }
                }

                // Fetch from remote
                ModLogger.Info($"Fetching from remote: {remoteUrl}");
                var data = await GetAsync<Dictionary<string, string>>(remoteUrl);
                if (data != null)
                {
                    SaveToFile(cachePath, data);
                }
                else
                {
                    // Remote fetch failed — fallback to local cache even if hash mismatched
                    ModLogger.Warn($"Remote fetch failed for {cacheKey}, trying local fallback.");
                    if (File.Exists(cachePath))
                    {
                        data = LoadFromFile(cachePath);
                        ModLogger.Info($"Loaded stale cache for {cacheKey}");
                        Toast.Warn("翻译服务", $"「{type}」无法更新，使用本地缓存");
                    }
                    else
                    {
                        Toast.Error("翻译服务", $"「{type}」加载失败，请检查网络");
                    }
                }
                return data;
            }
            finally
            {
                semaphore.Release();
                CleanupLocksIfNeeded();
            }
        }

        /// <summary>
        /// Periodically remove SemaphoreSlim entries that are no longer contended.
        /// Prevents unbounded growth of the _locks dictionary.
        /// </summary>
        private void CleanupLocksIfNeeded()
        {
            if (++_lockCleanupCounter % LockCleanupInterval != 0)
                return;

            // Only remove entries with no waiters — safe to remove idle semaphores
            var keysToRemove = new List<string>();
            foreach (var kvp in _locks)
            {
                if (kvp.Value.CurrentCount > 0) // no active waiters
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                if (_locks.TryRemove(key, out var sem) && sem.CurrentCount > 0)
                    sem.Dispose();
            }
        }

        /// <summary>
        /// Get the expected hash from the manifest for a given type/id combination.
        /// Returns null if the manifest doesn't contain this resource.
        /// </summary>
        private string GetManifestHash(string type, string id)
        {
            if (_manifest == null) return null;
            return type switch
            {
                "names" => _manifest.Names,
                "words" => _manifest.Words,
                "novels" when id != null =>
                    _manifest.Novels != null && _manifest.Novels.TryGetValue(id, out var hash)
                        ? hash
                        : null,
                _ => null,
            };
        }

        private string BuildRemoteUrl(string type, string id = null)
        {
            return type switch
            {
                "manifest" => $"{_cdn}/manifest/{_language}.json",
                "names"    => $"{_cdn}/names/{_language}.json",
                "words"    => $"{_cdn}/words/{_language}.json",
                "novels" when id != null => $"{_cdn}/novels/{id}/{_language}.json",
                "novels" => throw new ArgumentException("Novel ID is required for novels type"),
                _ => throw new ArgumentException($"Unknown translation type: {type}"),
            };
        }

        private string BuildCachePath(string type, string id = null)
        {
            var langDir = Path.Combine(_cacheDir, _language);
            return type switch
            {
                "manifest" => Path.Combine(langDir, "manifest.json"),
                "names"    => Path.Combine(langDir, "names.json"),
                "words"    => Path.Combine(langDir, "words.json"),
                "novels" when id != null => Path.Combine(langDir, "novels", $"{id}.json"),
                "novels" => throw new ArgumentException("Novel ID is required for novels type"),
                _ => throw new ArgumentException($"Unknown translation type: {type}"),
            };
        }

        private async Task<T> GetAsync<T>(string url) where T : class
        {
            try
            {
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception e)
            {
                ModLogger.Error($"HTTP GET error for {url}: {e.Message}");
            }
            return null;
        }

        private static Dictionary<string, string> LoadFromFile(string path)
        {
            try
            {
                var json = File.ReadAllText(path, Utf8);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch (Exception e)
            {
                ModLogger.Error($"Failed to load translation cache {path}: {e.Message}");
                return null;
            }
        }

        private static void SaveToFile(string path, Dictionary<string, string> data)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(data, HashJsonOptions);
            File.WriteAllText(path, json, Utf8);
        }

        #region Hash Methods

        /// <summary>
        /// Compute the canonical hash of a translation JSON file.
        /// Equivalent to Python's hash_file():
        ///   def hash_file(path):
        ///       return get_hash(json.loads(path.read_text(encoding="utf-8")))
        /// </summary>
        public static string HashFile(string path)
        {
            var json = File.ReadAllText(path, Utf8);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return GetHash(dict);
        }

        /// <summary>
        /// Compute the canonical hash of a dictionary.
        /// Equivalent to Python:
        ///   def get_hash(obj: dict[str, str]) -> str:
        ///       md5 = hashlib.md5()
        ///       for key in sorted(obj.keys()):
        ///           md5.update(f"{key}\x00{obj[key]}\x00".encode())
        ///       return md5.hexdigest()
        /// </summary>
        public static string GetHash(Dictionary<string, string> dict)
        {
            if (dict == null) return null;

            var sb = new StringBuilder();
            foreach (var key in dict.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                sb.Append(key);
                sb.Append('\0');
                sb.Append(dict[key]);
                sb.Append('\0');
            }
            var hash = MD5.HashData(Utf8.GetBytes(sb.ToString()));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        #endregion
    }
}
