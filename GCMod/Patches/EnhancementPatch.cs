using Gc;
using Gc.Battle.SkillWidget;
using HarmonyLib;

namespace GCMod.Patches;

/// <summary>
/// 游戏通用增强补丁：帧率修改 + 跳过大招动画。
/// </summary>
[HarmonyPatch]
public static class EnhancementPatch
{
    /// <summary>修改游戏帧率。</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GcOptionData), nameof(GcOptionData.SetPowerSaving))]
    public static void ChangeFrameRate()
    {
        int fps = Config.FrameRate.Value;
        if (fps > 0)
            GcOptionData.ChangeApplicationTargetFrameRate(fps);
    }

    /// <summary>跳过大招动画。</summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CutInMoviePlayer), nameof(CutInMoviePlayer.PlayAsync))]
    public static void SkipCutin(CutInMoviePlayer __instance)
    {
        if (Config.IsSkipCutin.Value)
            __instance.Skip();
    }
}
