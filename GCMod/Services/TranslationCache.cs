using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Utility.Notifications;

namespace GCMod.Services;

/// <summary>
/// 一个 CDN/语言会话的翻译缓存。合并资源请求，校验清单哈希，失败时回退到本地缓存。
/// HttpClient 由调用方持有；取消中的请求不会写入缓存。刷新时创建新实例。
/// </summary>
public sealed class TranslationCache
{
    private readonly string _cdn;
    private readonly string _cacheDir;
    private readonly string _language;
    private readonly HttpClient _client;
    private readonly TimeSpan _retryDelay;
    private volatile Manifest _manifest;

    // 不根据 CurrentCount 回收锁：等待者可能已取得引用。锁随缓存实例一起回收。
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<(string, Type), object> _memory = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _retryAfter = new();
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    /// <summary>创建缓存；无可用数据的请求默认冷却 30 秒后可重试。</summary>
    public TranslationCache(
        string cdn,
        string cacheDir,
        string language,
        HttpClient client,
        TimeSpan? retryDelay = null
    )
    {
        _cdn = cdn.TrimEnd('/');
        _cacheDir = cacheDir;
        _language = language;
        _client = client;
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>当前会话已加载的清单。</summary>
    public Manifest Manifest => _manifest;

    /// <summary>加载清单；失败时可在冷却结束后重试。</summary>
    public async Task<Manifest> FetchManifestAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await LoadAsync<Manifest>(
                TranslationPaths.Manifest,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        if (manifest != null)
            _manifest = manifest;
        return manifest;
    }

    /// <summary>加载人物名或剧情的原文到译文映射。</summary>
    public Task<Dictionary<string, string>> LoadAsync(
        string type,
        string id = null,
        CancellationToken cancellationToken = default
    ) => LoadAsync<Dictionary<string, string>>(type, id, cancellationToken);

    /// <summary>加载指定结构的翻译数据；缓存损坏视为未命中，磁盘写入失败不丢弃已下载的数据。</summary>
    public async Task<T> LoadAsync<T>(
        string type,
        string id = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        string key = id == null ? type : $"{type}/{id}";
        var memoryKey = (key, typeof(T));
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_memory.TryGetValue(memoryKey, out var saved))
                return (T)saved;

            if (
                _retryAfter.TryGetValue(key, out var nextAttempt)
                && DateTimeOffset.UtcNow < nextAttempt
            )
                return null;

            string path = TranslationPaths.BuildCachePath(_cacheDir, type, _language, id);
            string expectedHash = GetManifestHash(type, id);
            string localJson = await ReadLocalAsync(path, cancellationToken).ConfigureAwait(false);
            T local = Deserialize<T>(localJson, path);
            if (local != null && expectedHash != null && MatchesHash(localJson, expectedHash))
            {
                Logger.Info($"Cache hit: {_language}/{key}");
                _memory[memoryKey] = local;
                return local;
            }

            string url = TranslationPaths.BuildRemoteUrl(_cdn, type, _language, id);
            string remoteJson = await DownloadAsync(url, cancellationToken).ConfigureAwait(false);
            T remote = Deserialize<T>(remoteJson, url);
            if (remote != null && (expectedHash == null || MatchesHash(remoteJson, expectedHash)))
            {
                await SaveAsync(path, remoteJson, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                _memory[memoryKey] = remote;
                return remote;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (local != null)
            {
                Logger.Warn($"Using stale cache: {_language}/{key}");
                Toast.Warning("翻译服务", $"「{type}」无法更新，使用本地缓存");
                _memory[memoryKey] = local;
                return local;
            }

            _retryAfter[key] = DateTimeOffset.UtcNow + _retryDelay;
            Toast.Error("翻译服务", $"「{type}」加载失败，将在后续请求中重试");
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    private string GetManifestHash(string type, string id)
    {
        var manifest = _manifest;
        return type switch
        {
            TranslationPaths.Names => manifest?.Names,
            TranslationPaths.Master => manifest?.Master,
            TranslationPaths.Novels
                when id != null
                    && manifest?.Novels != null
                    && manifest.Novels.TryGetValue(id, out var hash) => hash,
            _ => null,
        };
    }

    private async Task<string> DownloadAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            Logger.Info($"Fetching translation: {url}");
            using var response = await _client
                .GetAsync(url, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response
                .Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
            when (e is HttpRequestException or OperationCanceledException or IOException)
        {
            Logger.Warn($"Translation download failed: {url}: {e.Message}");
            return null;
        }
    }

    private static async Task<string> ReadLocalAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        if (!File.Exists(path))
            return null;
        try
        {
            return await File.ReadAllTextAsync(path, Utf8, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Warn($"Cannot read translation cache: {path}: {e.Message}");
            return null;
        }
    }

    private static T Deserialize<T>(string json, string source)
        where T : class
    {
        if (json == null)
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException e)
        {
            Logger.Warn($"Invalid translation JSON: {source}: {e.Message}");
            return null;
        }
    }

    private static bool MatchesHash(string json, string expectedHash)
    {
        try
        {
            return TranslationHash.Compute(json) == expectedHash;
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException)
        {
            Logger.Warn($"Invalid translation hash input: {e.Message}");
            return false;
        }
    }

    private static async Task SaveAsync(
        string path,
        string json,
        CancellationToken cancellationToken
    )
    {
        string temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            await File.WriteAllTextAsync(temporary, json, Utf8, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Logger.Warn($"Cannot save translation cache: {path}: {e.Message}");
        }
        finally
        {
            try
            {
                File.Delete(temporary);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Logger.Warn($"Cannot remove temporary cache: {temporary}: {e.Message}");
            }
        }
    }
}
