using BepInEx.Configuration;
using UnityEngine;

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
                float delta = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ? 0.1f : -0.1f;
                Config.NormalAlpha.Value = Mathf.Clamp(Mathf.Round((Config.NormalAlpha.Value + delta) * 10f) / 10f, 0f, 1f);
                if (Patch.NormalFrame != null && Patch.BaseNameFrame != null)
                {
                    Color color1 = Patch.NormalFrame.color;
                    Color color2 = Patch.BaseNameFrame.color;
                    color1.a = color2.a = Config.NormalAlpha.Value;
                    Patch.NormalFrame.color = color1;
                    Patch.BaseNameFrame.color = color2;
                }
            }

            // F7: CgModeAlpha 调节
            if (Input.GetKeyDown(KeyCode.F7))
            {
                float delta = (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ? 0.1f : -0.1f;
                Config.CgModeAlpha.Value = Mathf.Clamp(Mathf.Round((Config.CgModeAlpha.Value + delta) * 10f) / 10f, 0f, 1f);
                if (Patch.CgModeFrame != null)
                {
                    Color color = Patch.CgModeFrame.color;
                    color.a = Config.CgModeAlpha.Value;
                    Patch.CgModeFrame.color = color;
                }
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
    }
}
