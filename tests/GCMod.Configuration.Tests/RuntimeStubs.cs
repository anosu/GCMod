namespace UnityEngine
{
    public class MonoBehaviour { }

    public readonly record struct Color(float r, float g, float b, float a);

    public enum KeyCode
    {
        F3,
        F5,
        F6,
        F7,
        F8,
        F10,
        LeftAlt,
        RightAlt,
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;

        public static bool GetKey(KeyCode key) => false;
    }

    public static class Mathf
    {
        public static float Round(float value) => MathF.Round(value);
    }
}

namespace Il2CppInterop.Runtime.Attributes
{
    public sealed class HideFromIl2CppAttribute : Attribute { }
}

namespace GCMod
{
    internal static class Plugin
    {
        internal static TestTranslations Trans = new();
    }

    internal sealed class TestTranslations
    {
        internal int Refreshes;
        internal int Loads;

        internal void Refresh() => Refreshes++;

        internal Task LoadTranslationAsync()
        {
            Loads++;
            return Task.CompletedTask;
        }
    }
}

namespace GCMod.Patches
{
    internal static class VisualPatch
    {
        internal static int Applied;

        internal static void ApplyWindowAlpha() => Applied++;
    }

    internal static class EnhancePatch
    {
        internal static int Applied;

        internal static void ChangeFrameRate() => Applied++;
    }
}
