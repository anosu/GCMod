using System.Collections.Generic;
using BepInEx.Configuration;
using GCMod.Patches;
using GCMod.Services;
using UnityEngine;

namespace GCMod;

/// <summary>
/// 快捷键处理。挂载为 MonoBehaviour，每帧检查按键输入。
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

        if (Input.GetKeyDown(KeyCode.F6) && CanTrigger(KeyCode.F6))
        {
            Config.NormalAlpha.Value = AlphaController.ClampAlpha(
                Config.NormalAlpha.Value + AlphaController.GetDelta(IsAltPressed()));
            AlphaController.ApplyAlpha(Config.NormalAlpha.Value,
                VisualPatch.NormalFrame, VisualPatch.BaseNameFrame);
        }

        if (Input.GetKeyDown(KeyCode.F7) && CanTrigger(KeyCode.F7))
        {
            Config.CgModeAlpha.Value = AlphaController.ClampAlpha(
                Config.CgModeAlpha.Value + AlphaController.GetDelta(IsAltPressed()));
            AlphaController.ApplyAlpha(Config.CgModeAlpha.Value, VisualPatch.CgModeFrame);
        }

        if (Input.GetKeyDown(KeyCode.F10) && CanTrigger(KeyCode.F10))
        {
            Plugin.ConfigFile.Reload();
            ModLogger.Info("Config reloaded");
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
