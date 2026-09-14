using System;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Hook;
using DMM.OLG.Unity.Engine;
using Gc;
using Gc.Master;
using HarmonyLib;
using Il2CppInterop.Runtime;
using BindingFlags = Il2CppSystem.Reflection.BindingFlags;
using MethodInfo = Il2CppSystem.Reflection.MethodInfo;

namespace GCMod.Patches;

/// <summary>
/// 在主数据解密、解压后，缓存和反序列化前读取或替换 JSON。
/// </summary>
[HarmonyPatch]
public static class MasterDataPatch
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void LoadCallbackFn(IntPtr self, IntPtr asset, IntPtr methodInfo);

    // 字段持有，避免原生钩子委托被 GC。
    static readonly LoadCallbackFn _loadHook = HookLoad;
    static LoadCallbackFn _loadOrig;
    static LoadCallbackFn _unhookedOrig;
    static INativeDetour _loadDetour;
    static Harmony _harmony;
    static readonly object Lifecycle = new();
    static int _activeLoads;

    [ThreadStatic]
    static IntPtr _pendingLoader;

    /// <summary>
    /// 接收 attribute 和未经修改的解密解压后 JSON。
    /// 返回替换 JSON；返回 null 或抛出异常时保留原文。
    /// 在游戏加载线程同步调用，处理器应支持不同线程并发调用。
    /// </summary>
    public static Func<MasterAttribute, string, string> JsonRewriter { get; set; }

    /// <summary>
    /// 安装加载上下文钩子和解压结果补丁；重复调用不会再次安装。
    /// </summary>
    public static void Install()
    {
        lock (Lifecycle)
        {
            if (_loadDetour != null)
                return;

            // DeserializeList 被内联到此回调，直接 hook DeserializeList 会漏掉列表加载。
            MethodInfo method = FindLoadCallback();
            if (method == null)
                return;

            IntPtr target = Marshal.ReadIntPtr(
                IL2CPP.il2cpp_method_get_from_reflection(method.Pointer)
            );
            if (target == IntPtr.Zero)
                throw new InvalidOperationException($"{method.Name} has no native method pointer");
            _unhookedOrig = Marshal.GetDelegateForFunctionPointer<LoadCallbackFn>(target);

            // 使用原生 trampoline，执行原函数时仍保留钩子，支持并发和嵌套加载。
            var detour = INativeDetour.Create(target, _loadHook);
            Harmony harmony = null;
            try
            {
                _loadOrig = detour.GenerateTrampoline<LoadCallbackFn>();
                harmony = Harmony.CreateAndPatchAll(typeof(MasterDataPatch));
                detour.Apply();
                _loadDetour = detour;
                _harmony = harmony;
            }
            catch
            {
                detour.Dispose();
                harmony?.UnpatchSelf();
                throw;
            }

            Plugin.Log.LogInfo(
                $"MasterDataPatch installed: {method.Name} @ 0x{target.ToInt64():X}"
            );
        }
    }

    /// <summary>无加载回调执行时撤销钩子；繁忙时保持原状并返回 false。</summary>
    public static bool TryUninstall()
    {
        lock (Lifecycle)
        {
            if (_activeLoads != 0)
                return false;
            if (_loadDetour == null)
                return true;

            _loadDetour.Undo();
            _harmony?.UnpatchSelf();
            // 已跳入托管回调、尚在等此锁的调用，改走恢复后的原生入口。
            _loadOrig = _unhookedOrig;
            _loadDetour.Dispose();
            _loadDetour = null;
            _harmony = null;
            _pendingLoader = IntPtr.Zero;
            return true;
        }
    }

    static MethodInfo FindLoadCallback()
    {
        // 使用原生类型的反射信息，不依赖 interop 包装方法名或 lambda 的具体编号。
        var matches = Il2CppType
            .Of<MasterLoader<ItemMaster>>()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(method =>
                method.IsPrivate
                && !method.IsGenericMethod
                && method.Name.StartsWith("<Load>b__", StringComparison.Ordinal)
                && method.ReturnType.FullName == "System.Void"
                && method.GetParameters() is { Length: 1 } parameters
                && parameters[0].ParameterType.FullName == "System.Object"
            )
            .ToArray();

        if (matches.Length == 1)
            return matches[0];

        Plugin.Log.LogError(
            $"[MasterDataPatch] Expected one Load callback, found {matches.Length}: {string.Join(", ", matches.Select(method => method.Name))}; patch disabled"
        );
        return null;
    }

    static void HookLoad(IntPtr self, IntPtr asset, IntPtr methodInfo)
    {
        LoadCallbackFn original;
        lock (Lifecycle)
        {
            _activeLoads++;
            original = _loadOrig;
        }
        IntPtr previous = _pendingLoader;
        _pendingLoader = self;
        try
        {
            original(self, asset, methodInfo);
        }
        finally
        {
            _pendingLoader = previous;
            lock (Lifecycle)
                _activeLoads--;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(TextCompressUtil),
        nameof(TextCompressUtil.Decompress),
        new[] { typeof(string) }
    )]
    static void OnDecompressed(ref string __result)
    {
        IntPtr loader = _pendingLoader;
        if (loader == IntPtr.Zero)
            return;

        // 每次加载只处理第一个解压结果，防止处理器或后续消费者的解压操作再次触发。
        _pendingLoader = IntPtr.Zero;
        try
        {
            IntPtr klass = IL2CPP.il2cpp_object_get_class(loader);
            IntPtr attributePointer = ReadFieldPointer(loader, klass, "attribute");
            var attribute =
                attributePointer == IntPtr.Zero ? null : new MasterAttribute(attributePointer);
            __result = JsonRewriter?.Invoke(attribute, __result) ?? __result;
        }
        catch (Exception e)
        {
            // 替换失败时 __result 尚未改变，游戏继续消费原始 JSON。
            Plugin.Log.LogError(
                $"[MasterDataPatch] JSON rewrite failed; keeping original JSON: {e}"
            );
        }
    }

    static IntPtr ReadFieldPointer(IntPtr instance, IntPtr klass, string fieldName)
    {
        IntPtr field = IL2CPP.GetIl2CppField(klass, fieldName);
        if (field == IntPtr.Zero)
            throw new MissingFieldException("MasterLoader", fieldName);

        int offset = checked((int)IL2CPP.il2cpp_field_get_offset(field));
        return Marshal.ReadIntPtr(instance + offset);
    }
}
