using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using GCMod.Patches;
using GCMod.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using TMPro;
using UnityEngine;
using Utility.Assets;
using Utility.Notifications;

namespace GCMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private const int HttpTimeoutSeconds = 30;
    private const int PooledConnectionLifetimeMinutes = 5;
    private const int PooledConnectionIdleTimeoutMinutes = 2;

    public static ConfigFile ConfigFile;
    public static new ManualLogSource Log;
    public static MonoBehaviour Instance;
    public static TranslationManager Trans;

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
        ConfigFile = base.Config;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        Toast.Initialize();
        GCMod.Config.Initialize();
        Instance = AddComponent<Hotkey>();

        Initialize();
        PatchManager.Initialize();
        Trans.Initialize();

        Toast.Success(
            MyPluginInfo.PLUGIN_NAME,
            $"Mod 加载成功，版本: {MyPluginInfo.PLUGIN_VERSION}"
        );
    }

    private static void Initialize()
    {
        var httpClient = new HttpClient(
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

        var cache = new TranslationCache(
            GCMod.Config.TranslationCDN.Value,
            Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID, "cache"),
            GCMod.Config.TranslationLanguage.Value,
            httpClient
        );

        string path = GCMod.Config.FontBundlePath.Value;
        string resolvedPath = Path.IsPathRooted(path) ? path : Path.Combine(Paths.PluginPath, path);

        var font = new AssetBundleLoader<TMP_FontAsset>(resolvedPath);
        Trans = new TranslationManager(cache, font);
    }

    public override bool Unload()
    {
        Toast.Clear();
        return base.Unload();
    }
}
