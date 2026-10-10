using System;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;
using Utility.Notifications;

namespace GCMod;

/// <summary>集中定义 BepInEx 配置、显示元数据和变更通知。</summary>
internal static class Config
{
    private const string DefaultTranslationCDN =
        "https://raw.githubusercontent.com/anosu/girlscreation-translation/refs/heads/main";
    private const string DefaultFontBundlePath =
        MyPluginInfo.PLUGIN_GUID + "/fonts/tsukuardgothic-std-medium";
    private static ConfigFile _config;

#if DEBUG
    private const string DefaultOfflineCDN = "http://localhost:33333/gc/";
    public static ConfigEntry<bool> Offline { get; private set; }
    public static ConfigEntry<string> OfflineCDN { get; private set; }
    public static bool OfflineStartup;
#endif

    public static ConfigEntry<int> FrameRate { get; private set; }
    public static ConfigEntry<bool> IsSkipCutin { get; private set; }
    public static ConfigEntry<bool> Translation { get; private set; }
    public static ConfigEntry<string> TranslationCDN { get; private set; }
    public static ConfigEntry<string> TranslationLanguage { get; private set; }
    public static ConfigEntry<bool> AsyncMode { get; private set; }
    public static ConfigEntry<string[]> MasterDataTables { get; private set; }
    public static ConfigEntry<string> FontBundlePath { get; private set; }
    public static ConfigEntry<float> NormalAlpha { get; private set; }
    public static ConfigEntry<float> CgModeAlpha { get; private set; }
    public static ConfigEntry<bool> ModifyText { get; private set; }
    public static ConfigEntry<string> NameTextColorHex { get; private set; }
    public static ConfigEntry<string> MessageTextColorHex { get; private set; }
    public static ConfigEntry<float> FaceDilate { get; private set; }
    public static ConfigEntry<string> OutlineColorHex { get; private set; }
    public static ConfigEntry<float> OutlineWidth { get; private set; }
    public static ConfigEntry<float> OutlineSoftness { get; private set; }
    public static ConfigEntry<float> CharacterSpacing { get; private set; }

    internal static Color NameTextColor => ReadColor(NameTextColorHex.Value);
    internal static Color MessageTextColor => ReadColor(MessageTextColorHex.Value);
    internal static Color OutlineColor => ReadColor(OutlineColorHex.Value);

    private static Color ReadColor(string value)
    {
        uint rgba = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new Color(
            (rgba >> 24) / 255f,
            ((rgba >> 16) & 255) / 255f,
            ((rgba >> 8) & 255) / 255f,
            (rgba & 255) / 255f
        );
    }

    internal static void Reload() => _config.Reload();

    public static void Initialize(ConfigFile config)
    {
        _config = config;
        if (!TomlTypeConverter.CanConvert(typeof(string[])))
            TomlTypeConverter.AddConverter(
                typeof(string[]),
                new TypeConverter
                {
                    ConvertToString = (value, _) => string.Join(", ", (string[])value),
                    ConvertToObject = (value, _) =>
                        value.Split(
                            ',',
                            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                        ),
                }
            );
        BindEntries(config);
#if DEBUG
        BindDebugEntries(config);
#endif
        config.SettingChanged += OnSettingChanged;
    }

    public static void Shutdown()
    {
        if (_config != null)
            _config.SettingChanged -= OnSettingChanged;
        _config = null;
    }

    private static void BindEntries(ConfigFile config)
    {
        FrameRate = config.Bind(
            "General",
            "FrameRate",
            60,
            new ConfigDescription(
                "游戏目标帧率，必须为正整数；即时生效。",
                new AcceptablePositiveInt(),
                new ConfigurationManagerAttributes { DispName = "目标帧率", Order = 100 }
            )
        );
        IsSkipCutin = config.Bind(
            "Battle",
            "IsSkipCutin",
            false,
            new ConfigDescription(
                "跳过大招的变身和释放动画；下一次播放动画时生效。快捷键：F3。",
                null,
                new ConfigurationManagerAttributes { DispName = "跳过大招动画", Order = 99 }
            )
        );
        Translation = config.Bind(
            "Translation",
            "Enabled",
            true,
            new ConfigDescription(
                "开启剧情与主数据翻译；开启时加载当前剧情，已写入的主数据译文需重启才能还原。快捷键：F8。",
                null,
                new ConfigurationManagerAttributes { DispName = "启用翻译", Order = 98 }
            )
        );
        TranslationCDN = config.Bind(
            "Translation",
            "CDN",
            DefaultTranslationCDN,
            new ConfigDescription(
                "翻译根地址，自动拼接 /translations/<Language>/；提交时去除首尾空格，无效地址使用默认值；修改后刷新翻译会话。",
                AcceptableResourceString.HttpUrl(DefaultTranslationCDN),
                new ConfigurationManagerAttributes
                {
                    DispName = "翻译 CDN",
                    Order = 97,
                    IsAdvanced = true,
                }
            )
        );
        TranslationLanguage = config.Bind(
            "Translation",
            "Language",
            "zh-Hans",
            new ConfigDescription(
                "翻译语言；修改后刷新翻译会话，已加载的主数据文本需重启恢复。",
                new AcceptableValueList<string>("zh-Hans"),
                new ConfigurationManagerAttributes { DispName = "翻译语言", Order = 96 }
            )
        );
        AsyncMode = config.Bind(
            "Translation",
            "AsyncMode",
            false,
            new ConfigDescription(
                "异步加载剧情翻译；关闭时剧情入口最多等待 10 秒，超时保留原文并继续后台加载；主数据始终采用此等待上限。",
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = "异步剧情翻译",
                    Order = 95,
                    IsAdvanced = true,
                }
            )
        );
        MasterDataTables = config.Bind(
            "Translation.MasterData",
            "EnabledTables",
            new[] { "*" },
            new ConfigDescription(
                "启用翻译的主数据表，逗号分隔；* 表示全部，留空表示关闭。区分大小写；对后续载入生效，已加载数据建议重启。",
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = "主数据表范围",
                    Order = 94,
                    IsAdvanced = true,
                }
            )
        );
        FontBundlePath = config.Bind(
            "Translation.Font",
            "AssetBundlePath",
            DefaultFontBundlePath,
            new ConfigDescription(
                "TMP 字体 AssetBundle 路径；相对路径以 BepInEx/plugins 为基准，也可填写绝对路径；提交时去除首尾空格，无效值使用默认值；修改后需重启游戏。",
                AcceptableResourceString.Path(DefaultFontBundlePath),
                new ConfigurationManagerAttributes
                {
                    DispName = "字体资源路径",
                    Order = 93,
                    IsAdvanced = true,
                }
            )
        );
        NormalAlpha = config.Bind(
            "Message.Window",
            "NormalAlpha",
            0f,
            new ConfigDescription(
                "普通剧情对话框不透明度，0 为完全透明；即时生效。F6 降低，Alt+F6 提高。",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { DispName = "普通剧情不透明度", Order = 92 }
            )
        );
        CgModeAlpha = config.Bind(
            "Message.Window",
            "CgModeAlpha",
            0f,
            new ConfigDescription(
                "寝室剧情对话框不透明度，0 为完全透明；即时生效。F7 降低，Alt+F7 提高。",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { DispName = "寝室剧情不透明度", Order = 91 }
            )
        );
        ModifyText = config.Bind(
            "Message.Text",
            "Modified",
            true,
            new ConfigDescription(
                "修改对话文本样式，提高透明背景下的可读性；后续显示文本时生效。快捷键：F5。",
                null,
                new ConfigurationManagerAttributes { DispName = "启用文本样式", Order = 90 }
            )
        );
        NameTextColorHex = config.Bind(
            "Message.Text",
            "NameColor",
            "FFFFFFFF",
            new ConfigDescription(
                "人物名文本颜色，RGBA 十六进制；后续显示文本时生效。",
                new AcceptableHexColor("FFFFFFFF"),
                new ConfigurationManagerAttributes { DispName = "人物名颜色", Order = 89 }
            )
        );
        MessageTextColorHex = config.Bind(
            "Message.Text",
            "MessageColor",
            "FFFFFFFF",
            new ConfigDescription(
                "消息文本颜色，RGBA 十六进制；后续显示文本时生效。",
                new AcceptableHexColor("FFFFFFFF"),
                new ConfigurationManagerAttributes { DispName = "消息颜色", Order = 88 }
            )
        );
        FaceDilate = config.Bind(
            "Message.Text",
            "FaceDilate",
            0.3f,
            new ConfigDescription(
                "字体粗细，范围 -1 至 1；后续显示文本时生效。",
                new AcceptableValueRange<float>(-1f, 1f),
                new ConfigurationManagerAttributes
                {
                    DispName = "字体粗细",
                    Order = 87,
                    ShowRangeAsPercent = false,
                }
            )
        );
        OutlineColorHex = config.Bind(
            "Message.Text",
            "OutlineColor",
            "3A3A3AFF",
            new ConfigDescription(
                "文本描边颜色，RGBA 十六进制；后续显示文本时生效。",
                new AcceptableHexColor("3A3A3AFF"),
                new ConfigurationManagerAttributes { DispName = "描边颜色", Order = 86 }
            )
        );
        OutlineWidth = config.Bind(
            "Message.Text",
            "OutlineWidth",
            0.3f,
            new ConfigDescription(
                "文本描边宽度；后续显示文本时生效。",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { DispName = "描边宽度", Order = 85 }
            )
        );
        OutlineSoftness = config.Bind(
            "Message.Text",
            "OutlineSoftness",
            0.01f,
            new ConfigDescription(
                "文本描边羽化程度；后续显示文本时生效。",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { DispName = "描边羽化", Order = 84 }
            )
        );
        CharacterSpacing = config.Bind(
            "Message.Text",
            "CharacterSpacing",
            0.1f,
            new ConfigDescription(
                "消息文本字间距，不应用于人物名；后续解析文本时生效。",
                null,
                new ConfigurationManagerAttributes { DispName = "消息字间距", Order = 83 }
            )
        );
    }

#if DEBUG
    private static void BindDebugEntries(ConfigFile config)
    {
        Offline = config.Bind(
            "Debug.Offline",
            "Enabled",
            false,
            new ConfigDescription(
                "将请求指向离线调试服务；下次请求时生效。",
                null,
                new ConfigurationManagerAttributes
                {
                    DispName = "离线调试",
                    Order = 100,
                    IsAdvanced = true,
                }
            )
        );
        OfflineCDN = config.Bind(
            "Debug.Offline",
            "CDN",
            DefaultOfflineCDN,
            new ConfigDescription(
                "离线调试 CDN 地址；提交时校验 HTTP/HTTPS 地址。",
                AcceptableResourceString.HttpUrl(DefaultOfflineCDN),
                new ConfigurationManagerAttributes
                {
                    DispName = "调试 CDN",
                    Order = 99,
                    IsAdvanced = true,
                }
            )
        );
    }
#endif

    private static void OnSettingChanged(object sender, SettingChangedEventArgs e)
    {
        var setting = e.ChangedSetting;
        object value = setting.BoxedValue is string[] values
            ? string.Join(", ", values)
            : setting.BoxedValue;
        Logger.Info($"[{setting.Definition.Section}] {setting.Definition.Key} => {value}");
        if (setting.SettingType == typeof(bool))
            Toast.Info($"[{setting.Definition.Section}]", $"{setting.Definition.Key} => {value}");
    }
}
