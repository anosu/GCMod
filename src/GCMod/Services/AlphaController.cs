using UnityEngine;

namespace GCMod.Services;

/// <summary>
/// 对话框透明度控制工具。
/// </summary>
public static class AlphaController
{
    private const float AlphaDelta = 0.1f;

    /// <summary>
    /// 将透明度值限制在 [0, 1] 范围内，并四舍五入到一位小数。
    /// </summary>
    public static float ClampAlpha(float value)
    {
        return Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 0f, 1f);
    }

    /// <summary>
    /// 根据 Alt 键状态获取透明度调整增量。
    /// </summary>
    /// <param name="altPressed">是否按住 Alt 键。</param>
    /// <returns>Alt 按下时增加透明度（+0.1），否则减少（-0.1）。</returns>
    public static float GetDelta(bool altPressed)
    {
        return altPressed ? AlphaDelta : -AlphaDelta;
    }

    /// <summary>
    /// 将透明度值应用到多个图片组件。
    /// </summary>
    /// <param name="alpha">目标透明度值（0-1）。</param>
    /// <param name="images">要应用透明度的图片组件数组。</param>
    public static void ApplyAlpha(float alpha, params UnityEngine.UI.Image[] images)
    {
        foreach (var image in images)
        {
            if (image == null)
                continue;
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
