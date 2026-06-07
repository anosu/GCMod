using TMPro;

namespace GCMod.Interfaces;

public interface IFontProvider
{
    TMP_FontAsset FontAsset { get; }
    bool IsFontValid();
    void EnsureLoaded();
}
