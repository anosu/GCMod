using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using GCMod.Patches;
using GCMod.Services;
using TMPro;
using UnityEngine;
using Utility.Assets;
using Utility.Notifications;

namespace GCMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BasePlugin
{
    private const int HttpTimeoutSeconds = 30;
    private const int PooledConnectionLifetimeMinutes = 5;
    private const int PooledConnectionIdleTimeoutMinutes = 2;
    private static HttpClient _httpClient;
    private static CancellationTokenSource _updateCheckCancellation;
    private static AssetBundleLoader<TMP_FontAsset> _font;
    private static IEnumerator _fontLoad;
    private static Coroutine _fontCoroutine;

    internal static new ManualLogSource Log { get; private set; }
    internal static MonoBehaviour Instance { get; private set; }
    internal static TranslationManager Trans { get; private set; }

    public override void Load()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch { }

#if DEBUG
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--offline") || args.Contains("-o"))
            GCMod.Config.OfflineStartup = true;
#endif

        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Toast.Initialize();
        GCMod.Config.Initialize(base.Config);
        Instance = AddComponent<RuntimeController>();

        try
        {
            Initialize();
            PatchManager.Initialize();
            InitializeFont();
            _ = Trans.LoadTranslationAsync();
        }
        catch
        {
            Unload();
            throw;
        }

        Toast.Success(
            MyPluginInfo.PLUGIN_NAME,
            $"Mod 加载成功，版本: {MyPluginInfo.PLUGIN_VERSION}"
        );
        _updateCheckCancellation = new CancellationTokenSource();
        _ = UpdateChecker.CheckAsync(
            _httpClient,
            MyPluginInfo.PLUGIN_VERSION,
            _updateCheckCancellation.Token
        );
    }

    private static void Initialize()
    {
        _httpClient = new HttpClient(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(PooledConnectionLifetimeMinutes),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(
                    PooledConnectionIdleTimeoutMinutes
                ),
            }
        )
        {
            Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds),
        };

        Trans = new TranslationManager(
            () =>
                new TranslationCache(
                    GCMod.Config.TranslationCDN.Value,
                    Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID, "translations"),
                    GCMod.Config.TranslationLanguage.Value,
                    _httpClient
                ),
            () =>
                new TranslationSettings(
                    GCMod.Config.Translation.Value,
                    GCMod.Config.AsyncMode.Value,
                    GCMod.Config.MasterDataTables.Value
                )
        );
        MasterDataPatch.JsonRewriter = (attribute, json) =>
            attribute == null ? json : Trans.TranslateMasterData(attribute.Object, json);
    }

    private static void InitializeFont()
    {
        string path = GCMod.Config.FontBundlePath.Value;
        _font = new AssetBundleLoader<TMP_FontAsset>(
            Path.IsPathRooted(path) ? path : Path.Combine(Paths.PluginPath, path)
        );
        _fontLoad = _font.Load(
            () =>
            {
                if (!TMP_Settings.fallbackFontAssets.Contains(_font.Asset))
                    TMP_Settings.fallbackFontAssets.Add(_font.Asset);
                Logger.Info($"Font loaded: {_font.Asset.name}");
            },
            error => Logger.Error($"Font load failed: {error.Message}")
        );
        _fontCoroutine = Instance.StartCoroutine(_fontLoad.WrapToIl2Cpp());
    }

    public override bool Unload()
    {
        if (!PatchManager.Uninstall())
        {
            Log.LogWarning(
                "Cannot unload while a master data callback is active; retry after loading finishes"
            );
            return false;
        }
        Trans?.Dispose();
        Trans = null;
        if (Instance != null)
        {
            Instance.enabled = false;
            if (_fontCoroutine != null)
                Instance.StopCoroutine(_fontCoroutine);
        }
        (_fontLoad as IDisposable)?.Dispose();
        if (_font?.IsLoaded == true)
            TMP_Settings.fallbackFontAssets.Remove(_font.Asset);
        _font = null;
        _fontLoad = null;
        _fontCoroutine = null;
        GCMod.Config.Shutdown();
        VisualPatch.Reset();
        MasterDataPatch.JsonRewriter = null;
        _updateCheckCancellation?.Cancel();
        _updateCheckCancellation?.Dispose();
        _updateCheckCancellation = null;
        _httpClient?.Dispose();
        _httpClient = null;
        if (Instance != null)
            UnityEngine.Object.Destroy(Instance);
        Instance = null;
        Toast.Clear();
        return true;
    }
}
