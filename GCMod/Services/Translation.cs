using BepInEx;
using BepInEx.Unity.IL2CPP.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
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
        public static Dictionary<string, string> names = [];
        public static Dictionary<string, string> words = [];
        public static Dictionary<int, Dictionary<string, string>> novels = [];
        public static AssetBundle fontBundle = null;
        public static TMP_FontAsset fontAsset = null;

        public static void Initialize()
        {
            cdn = Config.TranslationCDN.Value;
            Plugin.Instance.StartCoroutine(LoadFontAsset());
            _ = LoadTranslation();
        }

        public static async Task<T> GetAsync<T>(string url) where T : class
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Error: {e.Message}");
                ToastUI.Instance.Error("网络错误", e.Message);
            }
            return null;
        }

        public static void LoadFontBundle()
        {
            if (fontBundle != null) return;

            string path = Config.FontBundlePath.Value;
            string bundlePath = Path.IsPathRooted(path) ? path : Path.Combine(Paths.PluginPath, path);
            if (!File.Exists(bundlePath))
            {
                Plugin.Log.LogError("FontBundle path does not exist");
                ToastUI.Instance.Error("加载失败", "字体AB包路径不存在");
                return;
            }
            fontBundle = AssetBundle.LoadFromFile(bundlePath);
        }

        public static IEnumerator LoadFontAsset()
        {
            if (fontAsset != null || !Config.Translation.Value)
                yield break;

            LoadFontBundle();
            if (fontBundle == null)
            {
                Plugin.Log.LogError("Font bundle load failed");
                ToastUI.Instance.Error("加载失败", "字体AB包加载失败");
                yield break;
            }

            var request = fontBundle.LoadAssetAsync(Config.FontAssetName.Value);
            yield return request;

            fontAsset = request.asset.TryCast<TMP_FontAsset>();
            if (fontAsset == null)
            {
                Plugin.Log.LogError("TMP font asset load failed");
                ToastUI.Instance.Error("加载失败", "TMP字体资源加载失败");
            }
            else
            {
                Plugin.Log.LogInfo($"TMP_FontAsset {fontAsset.name} is loaded");
            }
        }

        public static async Task LoadTranslation()
        {
            if (!Config.Translation.Value) return;

            var nameTask = GetAsync<Dictionary<string, string>>($"{cdn}/names/zh_Hans.json");
            var wordTask = GetAsync<Dictionary<string, string>>($"{cdn}/words/zh_Hans.json");
            await Task.WhenAll(nameTask, wordTask);

            if (nameTask.Result != null)
            {
                names = nameTask.Result;
                Plugin.Log.LogInfo($"Character names translation loaded. Total: {names.Count}");
            }
            else
            {
                Plugin.Log.LogWarning("Character names translation load failed");
                ToastUI.Instance.Warn("加载失败", "角色名称翻译加载失败");
            }

            if (wordTask.Result != null)
            {
                words = wordTask.Result;
                Plugin.Log.LogInfo($"Character words translation loaded. Total: {words.Count}");
            }
            else
            {
                Plugin.Log.LogWarning("Character words translation load failed");
                ToastUI.Instance.Warn("加载失败", "角色台词翻译加载失败");
            }
        }

        public static async Task GetNovelTranslationAsync(int novelId)
        {
            if (novels.ContainsKey(novelId)) return;

            var translations = await GetAsync<Dictionary<string, string>>($"{cdn}/novels/{novelId}/zh_Hans.json");
            if (translations != null)
            {
                novels[novelId] = translations;
                Plugin.Log.LogInfo($"Scenario translation loaded. Total: {translations.Count}");
            }
            else
            {
                Plugin.Log.LogWarning($"Translations loaded failed: {novelId}");
                ToastUI.Instance.Warn("加载失败", $"剧本ID: {novelId}");
            }
        }
    }
}
