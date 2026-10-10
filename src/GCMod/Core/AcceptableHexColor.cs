#nullable enable
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;

namespace GCMod;

/// <summary>保持旧十六进制配置格式，统一为八位 RGBA；无需调用 Unity 原生颜色解析。</summary>
internal sealed class AcceptableHexColor : AcceptableValueBase
{
    private readonly string _defaultValue;

    internal AcceptableHexColor(string defaultValue)
        : base(typeof(string)) => _defaultValue = defaultValue;

    public override object Clamp(object value) => Normalize(value as string) ?? _defaultValue;

    public override bool IsValid(object value) => Normalize(value as string) != null;

    public override string ToDescriptionString() =>
        "# Acceptable values: RGB, RGBA, RRGGBB or RRGGBBAA hex colors";

    private static string? Normalize(string? value)
    {
        value = value?.Trim().TrimStart('#');
        if (value?.Length is 3 or 4)
            value = string.Concat(value.Select(character => new string(character, 2)));
        if (value?.Length == 6)
            value += "FF";
        return
            value?.Length == 8
            && uint.TryParse(
                value,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var rgba
            )
            ? rgba.ToString("X8", CultureInfo.InvariantCulture)
            : null;
    }
}
