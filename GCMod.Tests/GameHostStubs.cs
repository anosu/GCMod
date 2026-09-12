using System.Collections;

// 隔离 Unity/Harmony 宿主；剧情跟踪、刷新、查询与缓存执行实际生产代码。
namespace GCMod
{
    public sealed class TestSetting<T>(T value)
    {
        public T Value = value;
    }

    public static class Config
    {
        public static readonly TestSetting<bool> Translation = new(true);
        public static readonly TestSetting<bool> AsyncMode = new(false);
        public static readonly TestSetting<bool> ModifyText = new(false);
        public static readonly TestSetting<string[]> MasterDataTables = new([]);
        public static readonly TestSetting<float> CharacterSpacing = new(0);
    }

    public static class Plugin
    {
        public static Services.TranslationManager Trans;
        public static readonly TestLog Log = new();
        public static readonly UnityEngine.MonoBehaviour Instance = new();
    }

    public sealed class TestLog
    {
        public void LogInfo(string message) { }
    }
}

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HarmonyPatch : Attribute
    {
        public HarmonyPatch(params object[] arguments) { }
    }

    public sealed class HarmonyPrefix : Attribute { }

    public sealed class Harmony
    {
        public static Harmony CreateAndPatchAll(Type type) => new();

        public void UnpatchSelf() { }
    }
}

namespace UnityEngine
{
    public sealed class Coroutine { }

    public sealed class MonoBehaviour
    {
        public Coroutine StartCoroutine(object iterator) => new();

        public void StopCoroutine(Coroutine coroutine) { }
    }
}

namespace TMPro
{
    public sealed class TMP_FontAsset
    {
        public string name;
    }

    public static class TMP_Settings
    {
        public static readonly List<TMP_FontAsset> fallbackFontAssets = new();
    }
}

namespace Utility.Assets
{
    public sealed class AssetBundleLoader<T>
    {
        public T Asset;
        public bool IsLoaded => false;

        public IEnumerator Load(Action complete, Action<Exception> error)
        {
            yield break;
        }
    }
}

namespace BepInEx.Unity.IL2CPP.Utils.Collections
{
    public static class CoroutineExtensions
    {
        public static object WrapToIl2Cpp(this IEnumerator iterator) => iterator;
    }
}

namespace GCMod.Patches
{
    public sealed class EnhancePatch { }

    public sealed class VisualPatch { }

    public sealed class DebugPatch { }

    public static class MasterDataPatch
    {
        public static void Install() { }

        public static bool TryUninstall() => true;
    }
}

namespace Gc
{
    public sealed class Placeholder { }
}

namespace DMM.OLG.Unity.Engine.Internal
{
    public sealed class ScriptObjectManager
    {
        public void Create() { }
    }
}

namespace DMM.OLG.Unity.Extensions.Novel
{
    public sealed class Label
    {
        public string text;
    }

    public sealed class EventTitle
    {
        public readonly Label _TitleMain = new();

        public void ShowBlurEffect() { }
    }

    public sealed class EventMessage
    {
        public void SetName() { }
    }

    public sealed class EventText
    {
        public float fontSpacing;

        public void Parse() { }
    }
}
