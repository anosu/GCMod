using BepInEx.Configuration;
using UnityEngine;
using Utility.Toast;

namespace GCMod
{
    public static class Config
    {
#if DEBUG
        public static ConfigEntry<bool> Offline;
        public static ConfigEntry<string> OfflineCDN;
        public static bool OfflineStartup = false;
#endif
        public static ConfigEntry<int> FrameRate;
        public static ConfigEntry<bool> IsSkipCutin;
        public static ConfigEntry<bool> Translation;
        public static ConfigEntry<string> TranslationCDN;
        public static ConfigEntry<string> TranslationLanguage;
        public static ConfigEntry<bool> AsyncMode;
        public static ConfigEntry<string> FontBundlePath;
        public static ConfigEntry<string> FontAssetName;
        public static ConfigEntry<float> NormalAlpha;
        public static ConfigEntry<float> CgModeAlpha;
        public static ConfigEntry<bool> ModifyText;
        public static ConfigEntry<string> NameTextColorHex;
        public static ConfigEntry<string> MessageTextColorHex;
        public static ConfigEntry<float> FaceDilate;
        public static ConfigEntry<string> OutlineColorHex;
        public static ConfigEntry<float> OutlineWidth;
        public static ConfigEntry<float> OutlineSoftness;
        public static ConfigEntry<float> CharacterSpacing;

        public static Color NameTextColor = Color.white;
        public static Color MessageTextColor = Color.white;
        public static Color OutlineColor = new Color(0.235f, 0.235f, 0.235f);

        public static void Initialize()
        {
#if DEBUG
            Offline = Plugin.ConfigFile.Bind("Debug.Offline", "Enabled", false, "API localization for debug");
            OfflineCDN = Plugin.ConfigFile.Bind("Debug.Offline", "CDN", "http://localhost:33333/gc/", "CDN for debug");
#endif
            FrameRate = Plugin.ConfigFile.Bind("General", "FrameRate", 60, "游戏帧率（正整数）");
            IsSkipCutin = Plugin.ConfigFile.Bind("Battle", "IsSkipCutin", false, "是否跳过大招动画（包括变身和释放动画）");
            Translation = Plugin.ConfigFile.Bind("Translation", "Enabled", true, "是否开启游戏内剧情翻译");
            TranslationCDN = Plugin.ConfigFile.Bind("Translation", "CDN", "https://raw.githubusercontent.com/anosu/girlscreaionr-translation/refs/heads/main", "翻译加载的CDN");
            TranslationLanguage = Plugin.ConfigFile.Bind("Translation", "Language", "zh_Hans", "翻译语言，取值范围：[zh_Hans]");
            AsyncMode = Plugin.ConfigFile.Bind("Translation", "AsyncMode", false, "异步请求翻译（不会造成加载界面卡顿，但翻译可能延迟显示）");
            FontBundlePath = Plugin.ConfigFile.Bind("Translation.Font", "AssetBundlePath", "GCMod/fonts/TsukuARdGothic-Std-Bold", "TMP字体AssetBundle的路径，默认相对于插件目录，也可使用绝对路径");
            FontAssetName = Plugin.ConfigFile.Bind("Translation.Font", "AssetName", "TsukuARdGothic-Std-Bold SDF", "AssetBundle中TMP_FontAsset的名称");
            NormalAlpha = Plugin.ConfigFile.Bind("Message.Window", "NormalAlpha", 0f, "普通剧情中的对话框透明度，默认完全透明");
            CgModeAlpha = Plugin.ConfigFile.Bind("Message.Window", "CgModeAlpha", 0f, "寝室剧情中的对话框透明度，默认完全透明");
            ModifyText = Plugin.ConfigFile.Bind("Message.Text", "Modified", true, "是否更改对话框文本样式（用于对话框透明时提高对比度）");
            NameTextColorHex = Plugin.ConfigFile.Bind("Message.Text", "NameColor", "FFFFFFFF", "对话框人物名文本颜色");
            MessageTextColorHex = Plugin.ConfigFile.Bind("Message.Text", "MessageColor", "FFFFFFFF", "对话框消息文本颜色");
            FaceDilate = Plugin.ConfigFile.Bind("Message.Text", "FaceDilate", 0.3f, "字体粗细，取值范围：[-1, 1]");
            OutlineColorHex = Plugin.ConfigFile.Bind("Message.Text", "OutlineColor", "3A3A3AFF", "文本描边颜色，十六进制格式");
            OutlineWidth = Plugin.ConfigFile.Bind("Message.Text", "OutlineWidth", 0.3f, "文本描边宽度，取值范围：[0, 1]");
            OutlineSoftness = Plugin.ConfigFile.Bind("Message.Text", "OutlineSoftness", 0.01f, "文本描边羽化程度，取值范围：[0, 1]");
            CharacterSpacing = Plugin.ConfigFile.Bind("Message.Text", "CharacterSpacing", 0f, "字间距，仅对消息文本设置，不应用于人物名");

            ParseNameColor();
            ParseMessageColor();
            ParseOutlineColor();

            NameTextColorHex.SettingChanged += (_, _) => ParseNameColor();
            MessageTextColorHex.SettingChanged += (_, _) => ParseMessageColor();
            OutlineColorHex.SettingChanged += (_, _) => ParseOutlineColor();

            Plugin.ConfigFile.SettingChanged += (_, e) =>
            {
                var c = e.ChangedSetting;
                Plugin.Log.LogInfo($"[{c.Definition.Section}] {c.Definition.Key} => {c.BoxedValue}");
                Toast.Info($"[{c.Definition.Section}]", $"{c.Definition.Key} => {c.BoxedValue}");
            };
        }

        private static string NormalizeHexColor(string hex)
        {
            hex = hex.Trim().TrimStart('#');
            return "#" + hex;
        }

        private static void ParseNameColor()
        {
            if (ColorUtility.TryParseHtmlString(NormalizeHexColor(NameTextColorHex.Value), out Color c))
                NameTextColor = c;
        }

        private static void ParseMessageColor()
        {
            if (ColorUtility.TryParseHtmlString(NormalizeHexColor(MessageTextColorHex.Value), out Color c))
                MessageTextColor = c;
        }

        private static void ParseOutlineColor()
        {
            if (ColorUtility.TryParseHtmlString(NormalizeHexColor(OutlineColorHex.Value), out Color c))
                OutlineColor = c;
        }
    }
}
