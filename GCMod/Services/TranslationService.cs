using BepInEx;
using GCMod.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Utility.Toast;

namespace GCMod
{
    /// <summary>
    /// 翻译服务：管理翻译数据的加载、缓存和查询。
    /// 字体加载已迁移到 FontLoader。
    /// </summary>
    public static class TranslationService
    {
        public static string Cdn = "http://localhost:5000";

        public static readonly HttpClient Client = new(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        public static TranslationCache Cache;
        public static Dictionary<string, string> Names = [];
        public static Dictionary<string, string> Words = [];
        public static Dictionary<int, Dictionary<string, string>> Novels = [];

        public static void Initialize()
        {
            Cdn = Config.TranslationCDN.Value;
            var cacheDir = Path.Combine(Paths.PluginPath, "GCMod", "cache");
            Cache = new TranslationCache(Cdn, cacheDir, Config.TranslationLanguage.Value, Client);

            FontLoader.EnsureLoaded();
            _ = LoadTranslationAsync();
        }

        public static async Task LoadTranslationAsync()
        {
            if (!Config.Translation.Value) return;

            // Fetch manifest for cache validation
            await Cache.FetchManifestAsync();

            // Load names and words with cache-aware logic
            var nameTask = Cache.LoadAsync("names");
            var wordTask = Cache.LoadAsync("words");
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

        public static async Task GetNovelTranslationAsync(int novelId)
        {
            if (Novels.ContainsKey(novelId)) return;

            var translations = await Cache.LoadAsync("novels", novelId.ToString());
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
}
