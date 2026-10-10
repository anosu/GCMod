using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GCMod.Services;

internal readonly record struct TranslationSettings(
    bool Enabled,
    bool AsyncMode,
    string[] MasterDataTables
);

/// <summary>管理可替换的翻译会话，以及有等待上限的翻译消费。</summary>
internal sealed class TranslationManager : IDisposable
{
    private static readonly IReadOnlyDictionary<string, string> EmptyNames =
        new Dictionary<string, string>();
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);
    private readonly Func<TranslationCache> _createCache;
    private readonly Func<TranslationSettings> _getSettings;
    private TranslationSession _session;
    private bool _disposed;

    /// <summary>当前语言已加载的人物名。</summary>
    public IReadOnlyDictionary<string, string> Names =>
        Volatile.Read(ref _session)?.Names ?? EmptyNames;

    /// <summary>当前正在播放的剧本 ID；翻译关闭时仍继续跟踪。</summary>
    public int CurrentNovelId { get; private set; }

    public TranslationManager(
        Func<TranslationCache> createCache,
        Func<TranslationSettings> getSettings
    )
    {
        _createCache = createCache;
        _getSettings = getSettings;
        _session = new TranslationSession(createCache());
    }

    /// <summary>按当前 CDN/语言创建新会话，取消旧会话，重新验证翻译文件。</summary>
    public void Refresh()
    {
        if (_disposed)
            return;
        var replacement = new TranslationSession(_createCache());
        Interlocked.Exchange(ref _session, replacement)?.Dispose();
        _ = LoadTranslationAsync();
    }

    /// <summary>预加载人物名、主数据和当前剧本；取消旧会话时不弹出失败通知。</summary>
    public async Task LoadTranslationAsync()
    {
        var session = Volatile.Read(ref _session);
        var settings = _getSettings();
        if (session == null || !settings.Enabled)
            return;
        try
        {
            Task names = session.GetNamesAsync();
            Task master =
                settings.MasterDataTables.Length > 0
                    ? session.GetMasterAsync()
                    : Task.CompletedTask;
            Task novel =
                CurrentNovelId > 0 ? session.GetNovelAsync(CurrentNovelId) : Task.CompletedTask;
            await Task.WhenAll(names, master, novel).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Error($"Translation preload failed: {e.Message}");
        }
    }

    /// <summary>查询当前会话已加载的剧情翻译。</summary>
    public bool TryGetNovelTranslation(int id, out Dictionary<string, string> translations)
    {
        translations = null;
        return Volatile.Read(ref _session)?.TryGetNovel(id, out translations) == true;
    }

    /// <summary>始终记录当前剧本；翻译开启时加载译文，同步模式最多等待 10 秒。</summary>
    public void PrepareNovel(int id)
    {
        CurrentNovelId = id;
        var session = Volatile.Read(ref _session);
        var settings = _getSettings();
        if (session == null || !settings.Enabled)
            return;
        var task = session.GetNovelAsync(id);
        if (settings.AsyncMode)
            _ = ObserveAsync(task, $"novel/{id}");
        else
            WaitFor(task, $"novel/{id}");
    }

    /// <summary>替换已启用数据表的 JSON；最新翻译未就绪时先用本地缓存，无缓存才等待下载。</summary>
    public string TranslateMasterData(string tableName, string json)
    {
        var settings = _getSettings();
        string[] enabled = settings.MasterDataTables;
        var session = Volatile.Read(ref _session);
        if (
            session == null
            || !settings.Enabled
            || !MasterDataTranslator.IsEnabled(tableName, enabled)
        )
            return json;
        var translator = WaitFor(session.GetMasterForUseAsync(), "master");
        string result = translator?.Translate(tableName, json, enabled) ?? json;
        if (!ReferenceEquals(result, json))
            Logger.Info($"Master data translated: {tableName}");
        return result;
    }

    private static T WaitFor<T>(Task<T> task, string resource)
        where T : class
    {
        try
        {
            return task.WaitAsync(WaitLimit).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            Logger.Warn(
                $"Translation wait timed out: {resource}; keeping original text while loading continues"
            );
            _ = ObserveAsync(task, resource);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Error($"Translation load failed: {resource}: {e.Message}");
        }
        return null;
    }

    private static async Task ObserveAsync(Task task, string resource)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Logger.Error($"Translation load failed: {resource}: {e.Message}");
        }
    }

    /// <summary>取消当前会话，阻止旧请求继续发布数据。</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Interlocked.Exchange(ref _session, null)?.Dispose();
    }
}
