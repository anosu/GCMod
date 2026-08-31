using System.Collections.Generic;
using HarmonyLib;
using TMPro;

namespace GCMod.Patches;

/// <summary>
/// Harmony 补丁管理器。负责初始化所有子补丁类、提供共享工具方法。
/// </summary>
public static class PatchManager
{
    /// <summary>当前加载的剧情 Novel ID。</summary>
    public static int NovelId;

    /// <summary>
    /// 创建并注册所有 Harmony 补丁。
    /// </summary>
    public static void Initialize()
    {
        Harmony.CreateAndPatchAll(typeof(EnhancePatch));
        Harmony.CreateAndPatchAll(typeof(TranslationPatch));
        Harmony.CreateAndPatchAll(typeof(VisualPatch));
        Harmony.CreateAndPatchAll(typeof(HomePatch));
#if DEBUG
        Harmony.CreateAndPatchAll(typeof(DebugPatch));
#endif
    }

    /// <summary>
    /// 尝试获取当前 Novel ID 对应的翻译字典。
    /// </summary>
    public static bool TryGetCurrentNovel(out Dictionary<string, string> translation)
    {
        translation = null;
        return Config.Translation.Value
            && Plugin.Trans.Novels.TryGetValue(NovelId, out translation);
    }

    /// <summary>
    /// 对 TMP 文本组件应用翻译字体（带空安全检查）。
    /// </summary>
    public static void ApplyTranslationFont(TextMeshProUGUI text)
    {
        if (text != null && Plugin.Trans.Font.IsLoaded)
            text.font = Plugin.Trans.Font.Asset;
    }
}
