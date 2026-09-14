using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Utility.Caching;
using Utility.Notifications;

namespace GCMod.Services;

/// <summary>Preserves the game's translation protocol and notifications over the shared cache engine.</summary>
public sealed class TranslationCache
{
    private readonly string _cdn;
    private readonly string _cacheDir;
    private readonly string _language;
    private readonly TimeSpan _retryDelay;
    private readonly JsonResourceCache _cache;
    private volatile Manifest _manifest;

    /// <summary>Creates one CDN/language session; the caller owns the HTTP client.</summary>
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
        _retryDelay = retryDelay ?? TimeSpan.FromSeconds(30);
        _cache = new JsonResourceCache(
            client,
            TranslationHash.Compute,
            message => Logger.Info(message),
            message => Logger.Warn(message),
            OnFallback,
            retryDelay
        );
    }

    /// <summary>The manifest loaded for this session.</summary>
    public Manifest Manifest => _manifest;

    /// <summary>Loads the optional manifest using the same cache and retry policy.</summary>
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

    /// <summary>Loads a flat translation dictionary.</summary>
    public Task<Dictionary<string, string>> LoadAsync(
        string type,
        string id = null,
        CancellationToken cancellationToken = default
    ) => LoadAsync<Dictionary<string, string>>(type, id, cancellationToken);

    /// <summary>Reads a local snapshot without waiting for a remote request.</summary>
    public Task<T> LoadLocalAsync<T>(
        string type,
        string id = null,
        CancellationToken cancellationToken = default
    )
        where T : class =>
        _cache.LoadLocalAsync<T>(
            TranslationPaths.BuildCachePath(_cacheDir, type, _language, id),
            cancellationToken
        );

    /// <summary>Loads a resource with the game's path and manifest rules.</summary>
    public Task<T> LoadAsync<T>(
        string type,
        string id = null,
        CancellationToken cancellationToken = default
    )
        where T : class =>
        _cache.LoadAsync<T>(
            id == null ? type : $"{type}/{id}",
            TranslationPaths.BuildCachePath(_cacheDir, type, _language, id),
            TranslationPaths.BuildRemoteUrl(_cdn, type, _language, id),
            GetManifestHash(type, id),
            cancellationToken
        );

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

    private void OnFallback(string key, CacheFallback fallback)
    {
        var type = key.Split('/')[0];
        if (fallback == CacheFallback.Stale)
            Toast.Warning("翻译服务", $"「{type}」无法更新，使用本地缓存");
        else
            Toast.Error("翻译服务", $"「{type}」加载失败，将在后续请求中重试");
    }
}
