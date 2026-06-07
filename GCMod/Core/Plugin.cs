using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using GCMod.Patches;
using GCMod.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using UnityEngine;
using Utility.Toast;

namespace GCMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static ConfigFile ConfigFile;
    public static new ManualLogSource Log;
    public static MonoBehaviour Instance;

    public override void Load()
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

#if DEBUG
        var args = Environment.GetCommandLineArgs();
        if (args.Contains("--offline") || args.Contains("-o"))
            GCMod.Config.OfflineStartup = true;
#endif

        Log = base.Log;
        ConfigFile = base.Config;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        AddComponent<ToastUI>();
        GCMod.Config.Initialize();
        Initialize();
        PatchManager.Initialize();

        Instance = AddComponent<InputHandler>();
        Mod.Translation.Initialize();

        Toast.Success(MyPluginInfo.PLUGIN_NAME, $"Mod 加载成功，版本: {MyPluginInfo.PLUGIN_VERSION}");
    }

    private static void Initialize()
    {
        var httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        })
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        var cache = new TranslationCache(
            GCMod.Config.TranslationCDN.Value,
            System.IO.Path.Combine(Paths.PluginPath, "GCMod", "cache"),
            GCMod.Config.TranslationLanguage.Value,
            httpClient);

        var font = new FontLoader();
        var translation = new TranslationManager(cache, font, httpClient);

        Mod.Translation = translation;
        Mod.Font = font;
        Mod.Cache = cache;
    }

    public override bool Unload()
    {
        Toast.Clear();
        return base.Unload();
    }
}
