using Gc;
using Gc.Home;
using HarmonyLib;

namespace GCMod.Patches;

/// <summary>
/// 主页台词翻译与字体补丁。
/// </summary>
[HarmonyPatch]
public static class HomeWordPatch
{
    /// <summary>
    /// 翻译主页显示的角色台词。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitWordMasterBase), nameof(UnitWordMasterBase.Word), MethodType.Getter)]
    public static void SetHomeWord(ref string __result)
    {
        if (!Config.Translation.Value) return;

        if (TranslationService.Words.TryGetValue(__result, out string text))
            __result = text;
    }

    /// <summary>
    /// 设置或还原主页台词字体。
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HomeSpineObject), nameof(HomeSpineObject.PlayWord))]
    public static void SetHomeWordFont(HomeSpineObject __instance)
    {
        if (Config.Translation.Value)
        {
            if (!Services.FontLoader.IsFontValid())
                Services.FontLoader.EnsureLoaded();

            if (VisualPatch.OriginalFontAsset == null)
                VisualPatch.OriginalFontAsset = __instance._wordText.font;

            __instance._wordText.font = Services.FontLoader.FontAsset;
            __instance._wordText.lineSpacing = 24f;
            __instance._wordText.paragraphSpacing = 8f;
        }
        else
        {
            if (VisualPatch.OriginalFontAsset != null)
                __instance._wordText.font = VisualPatch.OriginalFontAsset;

            __instance._wordText.lineSpacing = 0f;
            __instance._wordText.paragraphSpacing = -16f;
        }
    }
}
