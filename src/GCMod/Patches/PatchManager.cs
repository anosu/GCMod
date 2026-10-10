using System.Collections.Generic;
using HarmonyLib;

namespace GCMod.Patches;

/// <summary>
/// 负责安装和撤销 Harmony 与原生补丁。
/// </summary>
public static class PatchManager
{
    private static readonly List<Harmony> Installed = new();

    /// <summary>
    /// 创建并注册所有 Harmony 补丁。
    /// </summary>
    public static void Initialize()
    {
        if (Installed.Count > 0)
            return;
        try
        {
            Installed.Add(Harmony.CreateAndPatchAll(typeof(EnhancePatch)));
            Installed.Add(Harmony.CreateAndPatchAll(typeof(TranslationPatch)));
            Installed.Add(Harmony.CreateAndPatchAll(typeof(VisualPatch)));
            MasterDataPatch.Install();
#if DEBUG
            Installed.Add(Harmony.CreateAndPatchAll(typeof(DebugPatch)));
#endif
        }
        catch
        {
            Uninstall();
            throw;
        }
    }

    /// <summary>撤销本插件安装的补丁；加载回调执行中时返回 false。</summary>
    public static bool Uninstall()
    {
        if (!MasterDataPatch.TryUninstall())
            return false;
        foreach (var harmony in Installed)
            harmony.UnpatchSelf();
        Installed.Clear();
        return true;
    }
}
