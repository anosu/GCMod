using System.Collections.Generic;
using Gc;
using Gc.Battle.SkillWidget;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace GCMod
{
    /// <summary>
    /// Harmony 补丁入口。负责初始化所有子补丁类、共享工具方法，
    /// 以及不属于特定功能域的通用补丁（跳过大招动画、修改帧率）。
    /// </summary>
    public static class Patch
    {
        /// <summary>当前加载的剧情 Novel ID。</summary>
        public static int NovelId;

        /// <summary>
        /// 创建并注册所有 Harmony 补丁。
        /// </summary>
        public static void Initialize()
        {
            Harmony.CreateAndPatchAll(typeof(Patch));
            Harmony.CreateAndPatchAll(typeof(Patches.TranslationPatch));
            Harmony.CreateAndPatchAll(typeof(Patches.VisualPatch));
            Harmony.CreateAndPatchAll(typeof(Patches.HomeWordPatch));
#if DEBUG
            Harmony.CreateAndPatchAll(typeof(Patches.DebugPatch));
#endif
        }

        /// <summary>
        /// 尝试获取当前 Novel ID 对应的翻译字典。
        /// </summary>
        public static bool TryGetCurrentNovel(out Dictionary<string, string> translation)
        {
            translation = null;
            return Config.Translation.Value && TranslationService.Novels.TryGetValue(NovelId, out translation);
        }

        /// <summary>
        /// 判断翻译字体是否已加载且有效（场景切换后自动重新校验）。
        /// </summary>
        public static bool HasTranslationFont()
        {
            return Config.Translation.Value && Services.FontLoader.IsFontValid();
        }

        /// <summary>
        /// 对 TMP 文本组件应用翻译字体。
        /// </summary>
        public static void ApplyTranslationFont(TextMeshProUGUI text)
        {
            if (text != null && HasTranslationFont())
                text.font = Services.FontLoader.FontAsset;
        }

        // ---- 通用补丁（不属于翻译/视觉/主页/调试） ----

        /// <summary>
        /// 跳过大招动画。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CutInMoviePlayer), nameof(CutInMoviePlayer.PlayAsync))]
        public static void SkipCutin(CutInMoviePlayer __instance)
        {
            if (Config.IsSkipCutin.Value)
                __instance.Skip();
        }

        /// <summary>
        /// 修改游戏帧率。
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GcOptionData), nameof(GcOptionData.SetPowerSaving))]
        public static void ChangeFrameRate()
        {
            int fps = Config.FrameRate.Value;
            if (fps > 0)
                GcOptionData.ChangeApplicationTargetFrameRate(fps);
        }
    }
}
