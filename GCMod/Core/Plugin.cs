using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System;
using System.Linq;
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
        Patch.Initialize();
        Instance = AddComponent<InputHandler>();
        Translation.Initialize();

        Toast.Success(MyPluginInfo.PLUGIN_NAME, $"Mod 加载成功，版本: {MyPluginInfo.PLUGIN_VERSION}");
    }

    public override bool Unload()
    {
        Toast.Clear();
        return base.Unload();
    }
}
