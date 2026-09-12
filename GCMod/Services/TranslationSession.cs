using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GCMod.Services;

/// <summary>
/// 一次 CDN/语言会话的翻译快照和加载任务。成功结果复用，失败结果允许重试。
/// 所有等待都不捕获调用线程上下文；释放时取消请求，并在任务结束后释放取消源。
/// </summary>
public sealed class TranslationSession : IDisposable, IAsyncDisposable
{
    private static readonly Dictionary<string, string> EmptyNames = new();
    private readonly TranslationCache _cache;
    private readonly object _gate = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _token;
    private readonly TaskCompletionSource _disposal = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );
    private readonly Dictionary<int, Task<Dictionary<string, string>>> _novels = new();
    private Task<Manifest> _manifest;
    private Task<Dictionary<string, string>> _names;
    private Task<MasterDataTranslator> _master;
    private Task<MasterDataTranslator> _cachedMaster;
    private bool _disposed;

    /// <summary>为不可变的 CDN/语言缓存创建一个会话。</summary>
    public TranslationSession(TranslationCache cache)
    {
        _cache = cache;
        _token = _lifetime.Token;
    }

    /// <summary>已加载的人物名快照；加载尚未完成时为空。</summary>
    public IReadOnlyDictionary<string, string> Names
    {
        get
        {
            lock (_gate)
                return Ready(_names) ?? EmptyNames;
        }
    }

    /// <summary>读取已经就绪的剧情，不发起网络请求。</summary>
    public bool TryGetNovel(int id, out Dictionary<string, string> translations)
    {
        lock (_gate)
        {
            _novels.TryGetValue(id, out var task);
            translations = Ready(task);
            return translations != null;
        }
    }

    /// <summary>合并人物名加载请求，失败后可再次调用。</summary>
    public Task<Dictionary<string, string>> GetNamesAsync()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _names = ReuseOrStart(
                _names,
                () => LoadAsync<Dictionary<string, string>>(TranslationPaths.Names)
            );
        }
    }

    /// <summary>合并指定剧情的加载请求，失败后可再次调用。</summary>
    public Task<Dictionary<string, string>> GetNovelAsync(int id)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _novels.TryGetValue(id, out var task);
            return _novels[id] = ReuseOrStart(
                task,
                () => LoadAsync<Dictionary<string, string>>(TranslationPaths.Novels, id.ToString())
            );
        }
    }

    /// <summary>加载并编译主数据翻译规则，成功后复用同一份规则。</summary>
    public Task<MasterDataTranslator> GetMasterAsync()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _master = ReuseOrStart(_master, LoadMasterAsync);
        }
    }

    /// <summary>消费时优先使用已就绪的最新版本，否则先用本地副本；后台更新继续执行。</summary>
    public async Task<MasterDataTranslator> GetMasterForUseAsync()
    {
        Task<MasterDataTranslator> latest = GetMasterAsync();
        if (Ready(latest) is { } ready)
            return ready;

        Task<MasterDataTranslator> cached;
        lock (_gate)
        {
            ThrowIfDisposed();
            cached = _cachedMaster = ReuseOrStart(_cachedMaster, LoadCachedMasterAsync);
        }
        var local = await cached.ConfigureAwait(false);
        _token.ThrowIfCancellationRequested();
        return Ready(latest) ?? local ?? await latest.ConfigureAwait(false);
    }

    private async Task<T> LoadAsync<T>(string type, string id = null)
        where T : class
    {
        Task<Manifest> manifest;
        lock (_gate)
            manifest = _manifest = ReuseOrStart(_manifest, () => _cache.FetchManifestAsync(_token));
        await manifest.ConfigureAwait(false);
        return await _cache.LoadAsync<T>(type, id, _token).ConfigureAwait(false);
    }

    private async Task<MasterDataTranslator> LoadMasterAsync()
    {
        var tables = await LoadAsync<
            Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        >(TranslationPaths.Master)
            .ConfigureAwait(false);
        return tables == null ? null : new MasterDataTranslator(tables);
    }

    private async Task<MasterDataTranslator> LoadCachedMasterAsync()
    {
        var tables = await _cache
            .LoadLocalAsync<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
                TranslationPaths.Master,
                cancellationToken: _token
            )
            .ConfigureAwait(false);
        return tables == null ? null : new MasterDataTranslator(tables);
    }

    private static T Ready<T>(Task<T> task)
        where T : class => task?.IsCompletedSuccessfully == true ? task.Result : null;

    private static Task<T> ReuseOrStart<T>(Task<T> task, Func<Task<T>> load)
        where T : class =>
        task != null && (!task.IsCompleted || Ready(task) != null) ? task : load();

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TranslationSession));
    }

    /// <summary>立即取消会话；旧请求的结果不会进入其他会话。</summary>
    public void Dispose()
    {
        Task[] pending;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            pending = _novels
                .Values.Cast<Task>()
                .Concat(new Task[] { _manifest, _names, _master, _cachedMaster })
                .Where(task => task != null)
                .ToArray();
        }
        _lifetime.Cancel();
        _ = FinishDisposalAsync(pending);
    }

    /// <summary>取消会话并等待全部请求释放资源。</summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return new ValueTask(_disposal.Task);
    }

    private async Task FinishDisposalAsync(Task[] pending)
    {
        try
        {
            await Task.WhenAll(pending).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Warn($"Translation session ended with an error: {e.Message}");
        }
        finally
        {
            _lifetime.Dispose();
            _disposal.TrySetResult();
        }
    }
}
