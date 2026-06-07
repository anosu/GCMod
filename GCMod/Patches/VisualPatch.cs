using DMM.OLG.Unity.Extensions.Novel;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GCMod.Patches;

/// <summary>
/// 视觉补丁：字体替换、文本样式修改、对话框透明度控制。
/// </summary>
[HarmonyPatch]
public static class VisualPatch
{
    public static Image NormalFrame;
    public static Image CgModeFrame;
    public static Image BaseNameFrame;
    public static readonly Color DefaultNameColor = new(0.957f, 0.957f, 0.914f);
    public static TMP_FontAsset OriginalFontAsset;

    private static void ApplyImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }

    /// <summary>
    /// 应用自定义文本样式（颜色、描边、间距）。
    /// </summary>
    public static void ModifyText(TextMeshProUGUI text, Color color)
    {
        text.color = color;
        text.fontMaterial.EnableKeyword("OUTLINE_ON");
        text.fontMaterial.SetFloat("_FaceDilate", Config.FaceDilate.Value);
        text.fontMaterial.SetColor("_OutlineColor", Config.OutlineColor);
        text.fontMaterial.SetFloat("_OutlineWidth", Config.OutlineWidth.Value);
        text.fontMaterial.SetFloat("_OutlineSoftness", Config.OutlineSoftness.Value);
    }

    /// <summary>
    /// 还原文本到默认样式。
    /// </summary>
    public static void CancelModifyText(TextMeshProUGUI text, Color color)
    {
        text.color = color;
        text.fontMaterial.SetFloat("_FaceDilate", 0f);
        text.fontMaterial.SetColor("_OutlineColor", Color.black);
        text.fontMaterial.SetFloat("_OutlineWidth", 0f);
        text.fontMaterial.SetFloat("_OutlineSoftness", 0f);
        text.fontMaterial.DisableKeyword("OUTLINE_ON");
    }

    /// <summary>
    /// 标题字体替换。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(EventTitle), nameof(EventTitle.ShowBlurEffect))]
    public static void SetMessageTitleFont(EventTitle __instance)
    {
        if (!Config.Translation.Value) return;
        if (Patch.TryGetCurrentNovel(out _))
            Patch.ApplyTranslationFont(__instance._TitleMain);
    }

    /// <summary>
    /// 人名字体替换 + 文本样式应用。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(EventMessage), nameof(EventMessage.SetName))]
    public static void SetMessageNameFont(EventMessage __instance)
    {
        if (Patch.TryGetCurrentNovel(out _))
            Patch.ApplyTranslationFont(__instance.MessageName);

        if (Config.ModifyText.Value)
            ModifyText(__instance.MessageName, Config.NameTextColor);
        else
            CancelModifyText(__instance.MessageName, DefaultNameColor);
    }

    /// <summary>
    /// 对话文本字体替换 + 样式应用。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(EventText), nameof(EventText.SetRuby))]
    public static void SetMessageTextFont(GameObject go, EventText.Letter letter, ref TextMeshProUGUI text)
    {
        if (Patch.TryGetCurrentNovel(out _))
            Patch.ApplyTranslationFont(text);

        if (Config.ModifyText.Value)
            ModifyText(text, Config.MessageTextColor);
    }

    /// <summary>
    /// 保存对话框背景图像引用，初始化透明度。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(EventMessage), nameof(EventMessage.Init))]
    public static void SaveTextBackground(EventMessage __instance)
    {
        foreach (Image image in __instance.GetComponentsInChildren<Image>())
        {
            if (image.name == "Normal")
            {
                ApplyImageAlpha(image, Config.NormalAlpha.Value);
                NormalFrame = image;
            }
            if (image.name == "MessageWindow")
            {
                ApplyImageAlpha(image, Config.CgModeAlpha.Value);
                CgModeFrame = image;
            }
        }
        foreach (Image image in __instance.NameImage.GetComponentsInChildren<Image>())
        {
            if (image.name == "BaseName")
            {
                ApplyImageAlpha(image, Config.NormalAlpha.Value);
                BaseNameFrame = image;
            }
        }
    }
}
