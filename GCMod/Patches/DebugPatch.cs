#if DEBUG
using DMM.OLG.Unity.Engine.Internal;
using HarmonyLib;

namespace GCMod.Patches;

/// <summary>
/// 调试补丁：离线模式 API 域名替换。
/// </summary>
[HarmonyPatch]
public static class DebugPatch
{
    /// <summary>
    /// 离线调试时替换 API 域名为本地 CDN。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ConfigData), nameof(ConfigData.Set))]
    public static void SetApiDomain(string key, ref string value)
    {
        if (!Config.OfflineStartup && !Config.Offline.Value)
            return;

        if ("ApiDomain".Equals(key))
        {
            value = Config.OfflineCDN.Value;
            Plugin.Log.LogInfo($"ApiDomain: {value}");
        }
    }
}
#endif
