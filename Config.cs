using BepInEx.Configuration;
using UnityEngine;

namespace GCMod
{
    public class Config
    {
        public static ConfigEntry<bool> Offline;
        public static ConfigEntry<string> OfflineCDN;
        public static ConfigEntry<bool> Translation;
        public static ConfigEntry<string> TranslationCDN;
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
            Offline = Plugin.Config.Bind("Debug.Offline", "Enabled", false, "API localization for debug");
            OfflineCDN = Plugin.Config.Bind("Debug.Offline", "CDN", "http://localhost:33333/gc", "CDN for debug");
            Translation = Plugin.Config.Bind("Translation", "Enabled", true, "是否开启游戏内剧情翻译");
            TranslationCDN = Plugin.Config.Bind("Translation", "CDN", "https://girlscreation.ntr.best", "翻译加载的CDN");
            AsyncMode = Plugin.Config.Bind("Translation", "AsyncMode", false, "异步请求翻译（不会造成加载界面卡顿，但翻译可能延迟显示）");
            FontBundlePath = Plugin.Config.Bind("Translation.Font", "AssetBundlePath", "font/TsukuARdGothic-Std-Bold", "TMP字体AssetBundle的路径，默认相对于插件目录，也可使用绝对路径");
            FontAssetName = Plugin.Config.Bind("Translation.Font", "AssetName", "TsukuARdGothic-Std-Bold SDF", "AssetBundle中TMP_FontAsset的名称");
            NormalAlpha = Plugin.Config.Bind("Message.Window", "NormalAlpha", 0f, "普通剧情中的对话框透明度，默认完全透明");
            CgModeAlpha = Plugin.Config.Bind("Message.Window", "CgModeAlpha", 0f, "寝室剧情中的对话框透明度，默认完全透明");
            ModifyText = Plugin.Config.Bind("Message.Text", "Modified", true, "是否更改对话框文本样式（用于对话框透明时提高对比度）");
            NameTextColorHex = Plugin.Config.Bind("Message.Text", "NameColor", "FFFFFFFF", "对话框人物名文本颜色");
            MessageTextColorHex = Plugin.Config.Bind("Message.Text", "MessageColor", "FFFFFFFF", "对话框消息文本颜色");
            FaceDilate = Plugin.Config.Bind("Message.Text", "FaceDilate", 0.3f, "字体粗细，取值范围：[-1, 1]");
            OutlineColorHex = Plugin.Config.Bind("Message.Text", "OutlineColor", "3A3A3AFF", "文本描边颜色，十六进制格式");
            OutlineWidth = Plugin.Config.Bind("Message.Text", "OutlineWidth", 0.3f, "文本描边宽度，取值范围：[0, 1]");
            OutlineSoftness = Plugin.Config.Bind("Message.Text", "OutlineSoftness", 0.01f, "文本描边羽化程度，取值范围：[0, 1]");
            CharacterSpacing = Plugin.Config.Bind("Message.Text", "CharacterSpacing", 0f, "字间距，仅对消息文本设置，不应用于人物名");

            ParseNameColor();
            ParseMessageColor();
            ParseOutlineColor();

            if (Offline.Value)
            {
                Plugin.Log.LogInfo($"Offline: {Offline.Value}");
                Plugin.Log.LogInfo($"OfflineCDN: {OfflineCDN.Value}");
            }
            Plugin.Log.LogInfo($"Translation: {Translation.Value}");
            Plugin.Log.LogInfo($"TranslationCDN: {TranslationCDN.Value}");
            Plugin.Log.LogInfo($"AsyncMode: {AsyncMode.Value}");
            Plugin.Log.LogInfo($"FontBundlePath: {FontBundlePath.Value}");
            Plugin.Log.LogInfo($"FontAssetName: {FontAssetName.Value}");
            Plugin.Log.LogInfo($"NormalAlpha: {NormalAlpha.Value}");
            Plugin.Log.LogInfo($"CgModeAlpha: {CgModeAlpha.Value}");
            Plugin.Log.LogInfo($"ModifyText: {ModifyText.Value}");
            Plugin.Log.LogInfo($"NameTextColor: {NormalizeHexColor(NameTextColorHex.Value)}");
            Plugin.Log.LogInfo($"MessageTextColor: {NormalizeHexColor(MessageTextColorHex.Value)}");
            Plugin.Log.LogInfo($"FaceDilate: {FaceDilate.Value}");
            Plugin.Log.LogInfo($"OutlineColor: {NormalizeHexColor(OutlineColorHex.Value)}");
            Plugin.Log.LogInfo($"OutlineWidth: {OutlineWidth.Value}");
            Plugin.Log.LogInfo($"OutlineSoftness: {OutlineSoftness.Value}");
            Plugin.Log.LogInfo($"CharacterSpacing: {CharacterSpacing.Value}");

            NameTextColorHex.SettingChanged += (sender, e) => ParseNameColor();
            MessageTextColorHex.SettingChanged += (sender, e) => ParseMessageColor();
            OutlineColorHex.SettingChanged += (sender, e) => ParseOutlineColor();

            Plugin.Config.SettingChanged += (sender, e) =>
            {
                var config = e.ChangedSetting;
                Plugin.Log.LogInfo($"[{config.Definition.Section}] {config.Definition.Key} => {config.BoxedValue}");
            };

        }

        public static string NormalizeHexColor(string hex)
        {
            hex = hex.Trim().TrimStart('#');
            return "#" + hex;
        }

        public static void ParseNameColor()
        {
            string color = NormalizeHexColor(NameTextColorHex.Value);
            if (ColorUtility.TryParseHtmlString(color, out Color nameColor))
            {
                NameTextColor = nameColor;
            }
        }

        public static void ParseMessageColor()
        {
            string color = NormalizeHexColor(MessageTextColorHex.Value);
            if (ColorUtility.TryParseHtmlString(color, out Color messageColor))
            {
                MessageTextColor = messageColor;
            }
        }

        public static void ParseOutlineColor()
        {
            string color = NormalizeHexColor(OutlineColorHex.Value);
            if (ColorUtility.TryParseHtmlString(color, out Color outlineColor))
            {
                OutlineColor = outlineColor;
            }
        }
    }
}
