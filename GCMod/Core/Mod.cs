using GCMod.Interfaces;

namespace GCMod;

/// <summary>
/// 轻量服务定位器。Plugin.Load() 负责装配，各模块只读。
/// </summary>
public static class Mod
{
    public static ITranslationProvider Translation { get; internal set; }
    public static IFontProvider Font { get; internal set; }
    public static TranslationCache Cache { get; internal set; }
}
