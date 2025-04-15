﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System;
using System.Text;

namespace GCMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static new ConfigFile Config;
    public static new ManualLogSource Log;

    public override void Load()
    {
        try {
            Console.OutputEncoding = Encoding.UTF8;
        } catch (Exception)
        {
        }

        // Plugin startup logic
        Log = base.Log;
        Config = base.Config;
        Log.LogInfo(new string('-', 50));
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Log.LogInfo("项目地址: https://github.com/anosu/GCMod");
        Log.LogInfo(new string('-', 50));
        GCMod.Config.Initialize();

        foreach (string text in Environment.GetCommandLineArgs())
        {
            if (text.Equals("-o"))
            {
                GCMod.Config.Offline.Value = true;
            }
            if (text.Equals("-t"))
            {
                GCMod.Config.Translation.Value = true;
            }
        }

        Patch.Initialize();
        Translation.Initialize();
        AddComponent<PluginBehaviour>();
    }
}
