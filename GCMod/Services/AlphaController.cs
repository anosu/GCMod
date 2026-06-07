using UnityEngine;

namespace GCMod.Services;

/// <summary>
/// 对话框透明度控制工具。
/// </summary>
public static class AlphaController
{
    public static float ClampAlpha(float value)
    {
        return Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 0f, 1f);
    }

    public static float GetDelta(bool altPressed)
    {
        return altPressed ? 0.1f : -0.1f;
    }

    public static void ApplyAlpha(float alpha, params UnityEngine.UI.Image[] images)
    {
        foreach (var image in images)
        {
            if (image == null) continue;
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
