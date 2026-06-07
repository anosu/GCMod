using GCMod.Interfaces;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Utility.Toast;

namespace GCMod.Services;

/// <summary>
/// 翻译管理器：协调翻译数据的加载、缓存和查询。
/// 实现 ITranslationProvider，内部持有所有翻译数据。
/// </summary>
public class TranslationManager : ITranslationProvider
{
    private readonly TranslationCache _cache;
    private readonly IFontProvider _font;

    public Dictionary<string, string> Names { get; private set; } = [];
    public Dictionary<string, string> Words { get; private set; } = [];
    public Dictionary<int, Dictionary<string, string>> Novels { get; private set; } = [];

    public TranslationManager(
        TranslationCache cache,
        IFontProvider font,
        HttpClient client)
    {
        _cache = cache;
        _font = font;
    }

    public void Initialize()
    {
        _font.EnsureLoaded();
        _ = LoadTranslationAsync();
    }

    public async Task LoadTranslationAsync()
    {
        if (!Config.Translation.Value) return;

        await _cache.FetchManifestAsync();

        var nameTask = _cache.LoadAsync("names");
        var wordTask = _cache.LoadAsync("words");
        await Task.WhenAll(nameTask, wordTask);

        if (nameTask.Result != null)
        {
            Names = nameTask.Result;
            ModLogger.Info($"Character names translation loaded. Total: {Names.Count}");
        }
        else
        {
            ModLogger.Warn("Character names translation load failed");
            Toast.Warn("加载失败", "角色名称翻译加载失败");
        }

        if (wordTask.Result != null)
        {
            Words = wordTask.Result;
            ModLogger.Info($"Character words translation loaded. Total: {Words.Count}");
        }
        else
        {
            ModLogger.Warn("Character words translation load failed");
            Toast.Warn("加载失败", "角色台词翻译加载失败");
        }
    }

    public async Task GetNovelTranslationAsync(int novelId)
    {
        if (Novels.ContainsKey(novelId)) return;

        var translations = await _cache.LoadAsync("novels", novelId.ToString());
        if (translations != null)
        {
            Novels[novelId] = translations;
            ModLogger.Info($"Scenario translation loaded. Total: {translations.Count}");
        }
        else
        {
            ModLogger.Warn($"Translations loaded failed: {novelId}");
            Toast.Warn("加载失败", $"剧本ID: {novelId}");
        }
    }
}
