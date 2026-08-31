using System;
using BepInEx.Configuration;
using UnityEngine;
using Utility.Notifications;

namespace GCMod
{
    /// <summary>
    /// 全局配置管理器。
    /// 负责初始化所有配置项并绑定事件监听。
    /// </summary>
    public static class Config
    {
        public static Color NameTextColor { get; private set; } = Color.white;
        public static Color MessageTextColor { get; private set; } = Color.white;
        public static Color OutlineColor { get; private set; } = new(0.235f, 0.235f, 0.235f);

#if DEBUG
        #region Debug
        public static ConfigEntry<bool> Offline;
        public static ConfigEntry<string> OfflineCDN;
        public static bool OfflineStartup;
        #endregion
#endif

        #region General
        public static ConfigEntry<int> FrameRate;
        #endregion

        #region Battle
        public static ConfigEntry<bool> IsSkipCutin;
        #endregion

        #region Translation
        public static ConfigEntry<bool> Translation;
        public static ConfigEntry<string> TranslationCDN;
        public static ConfigEntry<string> TranslationLanguage;
        public static ConfigEntry<bool> AsyncMode;
        #endregion

        #region Font
        public static ConfigEntry<string> FontBundlePath;
        #endregion

        #region MessageWindow
        public static ConfigEntry<float> NormalAlpha;
        public static ConfigEntry<float> CgModeAlpha;
        #endregion

        #region MessageText
        public static ConfigEntry<bool> ModifyText;
        public static ConfigEntry<string> NameTextColorHex;
        public static ConfigEntry<string> MessageTextColorHex;
        public static ConfigEntry<float> FaceDilate;
        public static ConfigEntry<string> OutlineColorHex;
        public static ConfigEntry<float> OutlineWidth;
        public static ConfigEntry<float> OutlineSoftness;
        public static ConfigEntry<float> CharacterSpacing;
        #endregion

        /// <summary>
        /// 初始化配置系统。
        /// </summary>
        public static void Initialize()
        {
            BindAllEntries();
            BindColorEntries();
            BindSettingChangedLog();
        }

        private static void BindAllEntries()
        {
#if DEBUG
            #region Debug
            Offline = Plugin.ConfigFile.Bind(
                "Debug.Offline",
                "Enabled",
                false,
                "API localization for debug"
            );
            OfflineCDN = Plugin.ConfigFile.Bind(
                "Debug.Offline",
                "CDN",
                "http://localhost:33333/gc/",
                "CDN for debug"
            );
            #endregion
#endif

            #region General
            FrameRate = Plugin.ConfigFile.Bind("General", "FrameRate", 60, "游戏帧率（正整数）");
            #endregion

            #region Battle
            IsSkipCutin = Plugin.ConfigFile.Bind(
                "Battle",
                "IsSkipCutin",
                false,
                "是否跳过大招动画（包括变身和释放动画）"
            );
            #endregion

            #region Translation
            Translation = Plugin.ConfigFile.Bind(
                "Translation",
                "Enabled",
                true,
                "是否开启游戏内剧情翻译"
            );
            TranslationCDN = Plugin.ConfigFile.Bind(
                "Translation",
                "CDN",
                "https://raw.githubusercontent.com/anosu/girlscreaionr-translation/refs/heads/main",
                "翻译加载的CDN"
            );
            TranslationLanguage = Plugin.ConfigFile.Bind(
                "Translation",
                "Language",
                "zh_Hans",
                "翻译语言，取值范围：[zh_Hans]"
            );
            AsyncMode = Plugin.ConfigFile.Bind(
                "Translation",
                "AsyncMode",
                false,
                "异步请求翻译（不会造成加载界面卡顿，但翻译可能延迟显示）"
            );
            #endregion

            #region Font
            FontBundlePath = Plugin.ConfigFile.Bind(
                "Translation.Font",
                "AssetBundlePath",
                $"{MyPluginInfo.PLUGIN_GUID}/fonts/TsukuARdGothic-Std-Bold",
                "TMP字体AssetBundle的路径，默认相对于插件目录，也可使用绝对路径"
            );
            #endregion

            #region MessageWindow
            NormalAlpha = Plugin.ConfigFile.Bind(
                "Message.Window",
                "NormalAlpha",
                0f,
                "普通剧情中的对话框透明度，默认完全透明"
            );
            CgModeAlpha = Plugin.ConfigFile.Bind(
                "Message.Window",
                "CgModeAlpha",
                0f,
                "寝室剧情中的对话框透明度，默认完全透明"
            );
            #endregion

            #region MessageText
            ModifyText = Plugin.ConfigFile.Bind(
                "Message.Text",
                "Modified",
                true,
                "是否更改对话框文本样式（用于对话框透明时提高对比度）"
            );
            NameTextColorHex = Plugin.ConfigFile.Bind(
                "Message.Text",
                "NameColor",
                "FFFFFFFF",
                "对话框人物名文本颜色"
            );
            MessageTextColorHex = Plugin.ConfigFile.Bind(
                "Message.Text",
                "MessageColor",
                "FFFFFFFF",
                "对话框消息文本颜色"
            );
            FaceDilate = Plugin.ConfigFile.Bind(
                "Message.Text",
                "FaceDilate",
                0.3f,
                "字体粗细，取值范围：[-1, 1]"
            );
            OutlineColorHex = Plugin.ConfigFile.Bind(
                "Message.Text",
                "OutlineColor",
                "3A3A3AFF",
                "文本描边颜色，十六进制格式"
            );
            OutlineWidth = Plugin.ConfigFile.Bind(
                "Message.Text",
                "OutlineWidth",
                0.3f,
                "文本描边宽度，取值范围：[0, 1]"
            );
            OutlineSoftness = Plugin.ConfigFile.Bind(
                "Message.Text",
                "OutlineSoftness",
                0.01f,
                "文本描边羽化程度，取值范围：[0, 1]"
            );
            CharacterSpacing = Plugin.ConfigFile.Bind(
                "Message.Text",
                "CharacterSpacing",
                0f,
                "字间距，仅对消息文本设置，不应用于人物名"
            );
            #endregion
        }

        /// <summary>
        /// 绑定配置变更日志输出。
        /// </summary>
        private static void BindSettingChangedLog()
        {
            Plugin.ConfigFile.SettingChanged += (_, e) =>
            {
                var c = e.ChangedSetting;
                Plugin.Log.LogInfo(
                    $"[{c.Definition.Section}] {c.Definition.Key} => {c.BoxedValue}"
                );
                Toast.Info($"[{c.Definition.Section}]", $"{c.Definition.Key} => {c.BoxedValue}");
            };
        }

        private static void BindColorEntries()
        {
            var bindings = new (ConfigEntry<string> Entry, Action<Color> Setter)[]
            {
                (NameTextColorHex, c => NameTextColor = c),
                (MessageTextColorHex, c => MessageTextColor = c),
                (OutlineColorHex, c => OutlineColor = c),
            };

            foreach (var (entry, setter) in bindings)
            {
                ParseAndSetColor(entry, setter);
                entry.SettingChanged += (_, _) => ParseAndSetColor(entry, setter);
            }
        }

        private static void ParseAndSetColor(ConfigEntry<string> entry, Action<Color> setter)
        {
            var hex = NormalizeHexColor(entry.Value);
            if (ColorUtility.TryParseHtmlString(hex, out var color))
                setter(color);
        }

        private static string NormalizeHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return "#FFFFFF";
            hex = hex.Trim().TrimStart('#');
            return "#" + hex;
        }
    }
}
