using System;
using System.Threading;
using BepInEx.Configuration;
using GCMod.Patches;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace GCMod;

/// <summary>统一处理快捷键及配置变更，将游戏对象更新限制在主线程。</summary>
public sealed class RuntimeController : MonoBehaviour
{
    private int _pending;
    private const int Window = 1,
        FrameRate = 2,
        RefreshTranslation = 4,
        LoadTranslation = 8;

    private void OnEnable()
    {
        Config.NormalAlpha.SettingChanged += OnChanged;
        Config.CgModeAlpha.SettingChanged += OnChanged;
        Config.FrameRate.SettingChanged += OnChanged;
        Config.Translation.SettingChanged += OnChanged;
        Config.MasterDataTables.SettingChanged += OnChanged;
        Config.TranslationCDN.SettingChanged += OnChanged;
        Config.TranslationLanguage.SettingChanged += OnChanged;
    }

    private void OnDisable()
    {
        Config.NormalAlpha.SettingChanged -= OnChanged;
        Config.CgModeAlpha.SettingChanged -= OnChanged;
        Config.FrameRate.SettingChanged -= OnChanged;
        Config.Translation.SettingChanged -= OnChanged;
        Config.MasterDataTables.SettingChanged -= OnChanged;
        Config.TranslationCDN.SettingChanged -= OnChanged;
        Config.TranslationLanguage.SettingChanged -= OnChanged;
        Interlocked.Exchange(ref _pending, 0);
    }

    [HideFromIl2Cpp]
    private void OnChanged(object sender, EventArgs e)
    {
        int change =
            ReferenceEquals(sender, Config.NormalAlpha)
            || ReferenceEquals(sender, Config.CgModeAlpha)
                ? Window
            : ReferenceEquals(sender, Config.FrameRate) ? FrameRate
            : ReferenceEquals(sender, Config.TranslationCDN)
            || ReferenceEquals(sender, Config.TranslationLanguage)
                ? RefreshTranslation
            : LoadTranslation;
        Interlocked.Or(ref _pending, change);
    }

    private void Update()
    {
        Toggle(KeyCode.F3, Config.IsSkipCutin);
        Toggle(KeyCode.F5, Config.ModifyText);
        Toggle(KeyCode.F8, Config.Translation);
        if (Input.GetKeyDown(KeyCode.F6))
            AdjustAlpha(Config.NormalAlpha);
        if (Input.GetKeyDown(KeyCode.F7))
            AdjustAlpha(Config.CgModeAlpha);
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Config.Reload();
            Interlocked.Or(ref _pending, RefreshTranslation);
            Logger.Info("Config reloaded");
        }

        int changes = Interlocked.Exchange(ref _pending, 0);
        if ((changes & Window) != 0)
            VisualPatch.ApplyWindowAlpha();
        if ((changes & FrameRate) != 0)
            EnhancePatch.ChangeFrameRate();
        if ((changes & RefreshTranslation) != 0)
            Plugin.Trans?.Refresh();
        else if ((changes & LoadTranslation) != 0 && Plugin.Trans != null)
            _ = Plugin.Trans.LoadTranslationAsync();
    }

    [HideFromIl2Cpp]
    private static void Toggle(KeyCode key, ConfigEntry<bool> entry)
    {
        if (Input.GetKeyDown(key))
            entry.Value = !entry.Value;
    }

    [HideFromIl2Cpp]
    private static void AdjustAlpha(ConfigEntry<float> entry)
    {
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        entry.Value = Mathf.Round((entry.Value + (alt ? 0.1f : -0.1f)) * 10f) / 10f;
    }
}
