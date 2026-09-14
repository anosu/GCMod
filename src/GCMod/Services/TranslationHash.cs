namespace GCMod.Services;

/// <summary>Preserves the existing nested translation manifest hash protocol.</summary>
internal static class TranslationHash
{
    public static string Compute(string json) => Utility.Cryptography.StringTableHash.Compute(json);
}
