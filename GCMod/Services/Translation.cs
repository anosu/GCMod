using BepInEx;
using BepInEx.Unity.IL2CPP.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Utility.Toast;

namespace GCMod
{
    public class Translation
    {
        public static string cdn = "http://localhost:5000";
        public static readonly HttpClient client = new();
        public static TranslationCache TranslationCache;
        public static Dictionary<string, string> names = [];
        public static Dictionary<string, string> words = [];
        public static Dictionary<int, Dictionary<string, string>> novels = [];
        public static AssetBundle fontBundle = null;
        public static TMP_FontAsset fontAsset = null;
        private static bool fontAssetLoading;

        public static void Initialize()
        {
            cdn = Config.TranslationCDN.Value;
            var cacheDir = Path.Combine(Paths.PluginPath, "GCMod", "cache");
            TranslationCache = new TranslationCache(cdn, cacheDir, Config.TranslationLanguage.Value, client);
            EnsureFontAssetLoading();
            _ = LoadTranslation();
        }

        public static void EnsureFontAssetLoading()
        {
            if (fontAsset != null || fontAssetLoading || !Config.Translation.Value)
                return;

            Plugin.Instance.StartCoroutine(LoadFontAsset());
        }

        public static void LoadFontBundle()
        {
            if (fontBundle != null) return;

            string path = Config.FontBundlePath.Value;
            string bundlePath = Path.IsPathRooted(path) ? path : Path.Combine(Paths.PluginPath, path);
            if (!File.Exists(bundlePath))
            {
                Plugin.Log.LogError("FontBundle path does not exist");
                Toast.Error("加载失败", "字体AB包路径不存在");
                return;
            }
            fontBundle = AssetBundle.LoadFromFile(bundlePath);
        }

        public static IEnumerator LoadFontAsset()
        {
            if (fontAsset != null || !Config.Translation.Value)
                yield break;

            fontAssetLoading = true;
            LoadFontBundle();
            if (fontBundle == null)
            {
                Plugin.Log.LogError("Font bundle load failed");
                Toast.Error("加载失败", "字体AB包加载失败");
                fontAssetLoading = false;
                yield break;
            }

            var request = fontBundle.LoadAssetAsync(Config.FontAssetName.Value);
            yield return request;

            fontAsset = request.asset.TryCast<TMP_FontAsset>();
            if (fontAsset == null)
            {
                Plugin.Log.LogError("TMP font asset load failed");
                Toast.Error("加载失败", "TMP字体资源加载失败");
            }
            else
            {
                Plugin.Log.LogInfo($"TMP_FontAsset {fontAsset.name} is loaded");
            }
            fontAssetLoading = false;
        }

        public static async Task LoadTranslation()
        {
            if (!Config.Translation.Value) return;

            // First fetch manifest to get expected hashes for cache validation
            await TranslationCache.FetchManifestAsync();

            // Then load names and words with cache-aware logic
            var nameTask = TranslationCache.LoadAsync("names");
            var wordTask = TranslationCache.LoadAsync("words");
            await Task.WhenAll(nameTask, wordTask);

            if (nameTask.Result != null)
            {
                names = nameTask.Result;
                Plugin.Log.LogInfo($"Character names translation loaded. Total: {names.Count}");
            }
            else
            {
                Plugin.Log.LogWarning("Character names translation load failed");
                Toast.Warn("加载失败", "角色名称翻译加载失败");
            }

            if (wordTask.Result != null)
            {
                words = wordTask.Result;
                Plugin.Log.LogInfo($"Character words translation loaded. Total: {words.Count}");
            }
            else
            {
                Plugin.Log.LogWarning("Character words translation load failed");
                Toast.Warn("加载失败", "角色台词翻译加载失败");
            }
        }

        public static async Task GetNovelTranslationAsync(int novelId)
        {
            if (novels.ContainsKey(novelId)) return;

            var translations = await TranslationCache.LoadAsync("novels", novelId.ToString());
            if (translations != null)
            {
                novels[novelId] = translations;
                Plugin.Log.LogInfo($"Scenario translation loaded. Total: {translations.Count}");
            }
            else
            {
                Plugin.Log.LogWarning($"Translations loaded failed: {novelId}");
                Toast.Warn("加载失败", $"剧本ID: {novelId}");
            }
        }
    }
}
