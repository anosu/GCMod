using System.Collections.Generic;
using BepInEx.Configuration;
using GCMod.Patches;
using GCMod.Services;
using UnityEngine;
using UnityEngine.UI;

namespace GCMod
{
    /// <summary>
    /// 快捷键输入处理。挂载为 MonoBehaviour，每帧检查按键输入。
    /// 使用节流机制避免连续帧重复触发同一快捷键。
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        private readonly Dictionary<KeyCode, float> _lastPressTime = new();
        private const float DebounceInterval = 0.15f;

        private void Update()
        {
            CheckToggle(KeyCode.F3, () => Config.IsSkipCutin);
            CheckToggle(KeyCode.F5, () => Config.ModifyText);
            CheckToggle(KeyCode.F8, () => Config.Translation);

            // F6: NormalAlpha 调节
            if (Input.GetKeyDown(KeyCode.F6) && CanTrigger(KeyCode.F6))
            {
                Config.NormalAlpha.Value = AlphaController.ClampAlpha(
                    Config.NormalAlpha.Value + AlphaController.GetDelta(IsAltPressed()));
                AlphaController.ApplyAlpha(Config.NormalAlpha.Value,
                    VisualPatch.NormalFrame, VisualPatch.BaseNameFrame);
            }

            // F7: CgModeAlpha 调节
            if (Input.GetKeyDown(KeyCode.F7) && CanTrigger(KeyCode.F7))
            {
                Config.CgModeAlpha.Value = AlphaController.ClampAlpha(
                    Config.CgModeAlpha.Value + AlphaController.GetDelta(IsAltPressed()));
                AlphaController.ApplyAlpha(Config.CgModeAlpha.Value, VisualPatch.CgModeFrame);
            }

            // F10: 重载配置
            if (Input.GetKeyDown(KeyCode.F10) && CanTrigger(KeyCode.F10))
            {
                Plugin.ConfigFile.Reload();
                Plugin.Log.LogInfo("Config reloaded");
            }
        }

        private void CheckToggle(KeyCode key, System.Func<ConfigEntry<bool>> getter)
        {
            if (Input.GetKeyDown(key) && CanTrigger(key))
            {
                var entry = getter();
                entry.Value = !entry.Value;
            }
        }

        /// <summary>
        /// 防抖检查：同一按键在 DebounceInterval 内只能触发一次。
        /// </summary>
        private bool CanTrigger(KeyCode key)
        {
            float now = Time.time;
            if (_lastPressTime.TryGetValue(key, out float last) && now - last < DebounceInterval)
                return false;
            _lastPressTime[key] = now;
            return true;
        }

        private static bool IsAltPressed()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }
    }
}
