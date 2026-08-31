using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using TMPro;
using Utility.Assets;
using Utility.Notifications;

namespace GCMod.Services;

/// <summary>
/// 翻译管理器：协调翻译数据的加载、缓存和查询。
/// 内部持有所有翻译数据。
/// </summary>
public class TranslationManager
{
    private readonly TranslationCache _cache;
    private readonly AssetBundleLoader<TMP_FontAsset> _font;

    public Dictionary<string, string> Names { get; private set; } = [];
    public Dictionary<string, string> Words { get; private set; } = [];
    public Dictionary<int, Dictionary<string, string>> Novels { get; private set; } = [];
    public AssetBundleLoader<TMP_FontAsset> Font => _font;

    public TranslationManager(TranslationCache cache, AssetBundleLoader<TMP_FontAsset> font)
    {
        _cache = cache;
        _font = font;
    }

    public void Initialize()
    {
        Plugin.Instance.StartCoroutine(_font.Load().WrapToIl2Cpp());
        _ = LoadTranslationAsync();
    }

    public async Task LoadTranslationAsync()
    {
        if (!Config.Translation.Value)
            return;

        await _cache.FetchManifestAsync();

        var nameTask = _cache.LoadAsync(TranslationPaths.Names);
        var wordTask = _cache.LoadAsync(TranslationPaths.Words);
        await Task.WhenAll(nameTask, wordTask);

        if (nameTask.Result != null)
        {
            Names = nameTask.Result;
            Logger.Info($"Character names translation loaded. Total: {Names.Count}");
        }
        else
        {
            Logger.Warn("Character names translation load failed");
            Toast.Warning("加载失败", "角色名称翻译加载失败");
        }

        if (wordTask.Result != null)
        {
            Words = wordTask.Result;
            Logger.Info($"Character words translation loaded. Total: {Words.Count}");
        }
        else
        {
            Logger.Warn("Character words translation load failed");
            Toast.Warning("加载失败", "角色台词翻译加载失败");
        }
    }

    public async Task GetNovelTranslationAsync(int novelId)
    {
        if (Novels.ContainsKey(novelId))
            return;

        var translations = await _cache.LoadAsync(TranslationPaths.Novels, novelId.ToString());
        if (translations != null)
        {
            Novels[novelId] = translations;
            Logger.Info($"Scenario translation loaded. Total: {translations.Count}");
        }
        else
        {
            Logger.Warn($"Translations loaded failed: {novelId}");
            Toast.Warning("加载失败", $"剧本ID: {novelId}");
        }
    }
}
