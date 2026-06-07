using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace GCMod
{
    /// <summary>快捷键输入处理</summary>
    public class InputHandler : MonoBehaviour
    {
        private void Update()
        {
            CheckToggle(KeyCode.F3, () => Config.IsSkipCutin);
            CheckToggle(KeyCode.F5, () => Config.ModifyText);
            CheckToggle(KeyCode.F8, () => Config.Translation);

            // F6: NormalAlpha 调节
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Config.NormalAlpha.Value = ClampAlpha(Config.NormalAlpha.Value + GetAlphaDelta());
                ApplyAlpha(Config.NormalAlpha.Value, Patch.NormalFrame, Patch.BaseNameFrame);
            }

            // F7: CgModeAlpha 调节
            if (Input.GetKeyDown(KeyCode.F7))
            {
                Config.CgModeAlpha.Value = ClampAlpha(Config.CgModeAlpha.Value + GetAlphaDelta());
                ApplyAlpha(Config.CgModeAlpha.Value, Patch.CgModeFrame);
            }

            // F10: 重载配置
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Plugin.ConfigFile.Reload();
                Plugin.Log.LogInfo("Config reloaded");
            }
        }

        private static void CheckToggle(KeyCode key, System.Func<ConfigEntry<bool>> getter)
        {
            if (Input.GetKeyDown(key))
            {
                var entry = getter();
                entry.Value = !entry.Value;
            }
        }

        private static bool IsAltPressed()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        private static float GetAlphaDelta()
        {
            return IsAltPressed() ? 0.1f : -0.1f;
        }

        private static float ClampAlpha(float value)
        {
            return Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 0f, 1f);
        }

        private static void ApplyAlpha(float alpha, params Image[] images)
        {
            foreach (var image in images)
            {
                if (image == null)
                    continue;

                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }
        }
    }
}
