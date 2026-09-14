using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using TMPro;
using UnityEngine;
using Utility.Assets;

namespace GCMod.Services;

/// <summary>协调游戏线程上的字体、配置刷新和有时限的翻译消费。</summary>
public sealed class TranslationManager : IDisposable
{
    private static readonly IReadOnlyDictionary<string, string> EmptyNames =
        new Dictionary<string, string>();
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);
    private readonly Func<TranslationCache> _createCache;
    private readonly AssetBundleLoader<TMP_FontAsset> _font;
    private TranslationSession _session;
    private IEnumerator _fontLoad;
    private Coroutine _fontCoroutine;
    private bool _disposed;

    /// <summary>当前语言已加载的人物名。</summary>
    public IReadOnlyDictionary<string, string> Names =>
        Volatile.Read(ref _session)?.Names ?? EmptyNames;
    public AssetBundleLoader<TMP_FontAsset> Font => _font;

    /// <summary>当前正在播放的剧本 ID；翻译关闭时仍继续跟踪。</summary>
    public int CurrentNovelId { get; private set; }

    public TranslationManager(
        Func<TranslationCache> createCache,
        AssetBundleLoader<TMP_FontAsset> font
    )
    {
        _createCache = createCache;
        _font = font;
        _session = new TranslationSession(createCache());
    }

    /// <summary>加载字体，并提前请求人物名与启用的主数据翻译。</summary>
    public void Initialize()
    {
        _fontLoad = _font.Load(
            () =>
            {
                if (!_disposed && !TMP_Settings.fallbackFontAssets.Contains(_font.Asset))
                {
                    TMP_Settings.fallbackFontAssets.Add(_font.Asset);
                    Logger.Info($"Font loaded: {_font.Asset.name}");
                }
            },
            error => Logger.Error($"Font load failed: {error.Message}")
        );
        _fontCoroutine = Plugin.Instance.StartCoroutine(_fontLoad.WrapToIl2Cpp());
        _ = LoadTranslationAsync();
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
        if (session == null || !Config.Translation.Value)
            return;
        try
        {
            Task names = session.GetNamesAsync();
            Task master =
                Config.MasterDataTables.Value.Length > 0
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
        if (session == null || !Config.Translation.Value)
            return;
        var task = session.GetNovelAsync(id);
        if (Config.AsyncMode.Value)
            _ = ObserveAsync(task, $"novel/{id}");
        else
            WaitFor(task, $"novel/{id}");
    }

    /// <summary>替换已启用数据表的 JSON；最新翻译未就绪时先用本地缓存，无缓存才等待下载。</summary>
    public string TranslateMasterData(string tableName, string json)
    {
        string[] enabled = Config.MasterDataTables.Value;
        var session = Volatile.Read(ref _session);
        if (
            session == null
            || !Config.Translation.Value
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

    /// <summary>取消翻译请求和字体协程，撤销全局字体后备注册。</summary>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Interlocked.Exchange(ref _session, null)?.Dispose();
        if (_fontCoroutine != null)
            Plugin.Instance.StopCoroutine(_fontCoroutine);
        (_fontLoad as IDisposable)?.Dispose();
        if (_font.IsLoaded)
            TMP_Settings.fallbackFontAssets.Remove(_font.Asset);
        // 已赋给游戏组件的字体仍可能被使用，不在此销毁 Unity 对象。
    }
}
