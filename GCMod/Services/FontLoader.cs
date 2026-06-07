using BepInEx;
using BepInEx.Unity.IL2CPP.Utils;
using GCMod.Interfaces;
using System.Collections;
using TMPro;
using UnityEngine;
using Utility.Toast;

namespace GCMod.Services;

/// <summary>
/// 翻译字体加载器，负责 AssetBundle 加载和 TMP_FontAsset 创建。
/// 字体资源在加载后会标记为跨场景持久化，场景切换不会失效。
/// </summary>
public class FontLoader : IFontProvider
{
    public AssetBundle FontBundle { get; private set; }
    public TMP_FontAsset FontAsset { get; private set; }
    private bool _isLoading;

    /// <summary>
    /// 确保字体已加载且有效。若已加载但场景切换导致失效，自动重新加载。
    /// </summary>
    public void EnsureLoaded()
    {
        if (!Config.Translation.Value) return;

        // 已加载且有效 → 无需操作
        if (IsFontValid()) return;

        // 正在加载中 → 等待完成
        if (_isLoading) return;

        // 之前加载过但已失效 → 清理旧引用后重新加载
        if (FontAsset != null)
        {
            ModLogger.Warn("Font asset became invalid (scene change?), reloading...");
            FontAsset = null;
        }

        Plugin.Instance.StartCoroutine(LoadFontAssetCoroutine());
    }

    /// <summary>
    /// 校验字体资源是否真实有效（不仅仅是非 null）。
    /// 场景切换后 Unity 可能销毁底层纹理/材质，导致 C# 引用变成"假空"。
    /// </summary>
    public bool IsFontValid()
    {
        if (FontAsset == null) return false;

        // TMP_FontAsset 有效性的关键指标：atlasTexture 和 material 是否存在
        if (FontAsset.atlasTexture == null) return false;
        if (FontAsset.material == null) return false;

        return true;
    }

    public void LoadFontBundle(string bundlePath)
    {
        if (FontBundle != null) return;

        if (!System.IO.File.Exists(bundlePath))
        {
            ModLogger.Error("FontBundle path does not exist");
            Toast.Error("加载失败", "字体AB包路径不存在");
            return;
        }
        FontBundle = AssetBundle.LoadFromFile(bundlePath);
    }

    private IEnumerator LoadFontAssetCoroutine()
    {
        if (IsFontValid() || !Config.Translation.Value)
            yield break;

        _isLoading = true;

        string path = Config.FontBundlePath.Value;
        string resolvedPath = System.IO.Path.IsPathRooted(path)
            ? path
            : System.IO.Path.Combine(BepInEx.Paths.PluginPath, path);

        LoadFontBundle(resolvedPath);

        if (FontBundle == null)
        {
            ModLogger.Error("Font bundle load failed");
            Toast.Error("加载失败", "字体AB包加载失败");
            _isLoading = false;
            yield break;
        }

        var request = FontBundle.LoadAssetAsync(Config.FontAssetName.Value);
        yield return request;

        FontAsset = request.asset.TryCast<TMP_FontAsset>();
        if (FontAsset == null)
        {
            ModLogger.Error("TMP font asset load failed");
            Toast.Error("加载失败", "TMP字体资源加载失败");
        }
        else
        {
            // 标记为跨场景持久化，防止场景切换时被卸载
            FontAsset.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(FontAsset);
            FontBundle.Unload(false); // 卸载AB包但保留已加载的资源
            ModLogger.Info($"TMP_FontAsset {FontAsset.name} is loaded and marked persistent");
        }
        _isLoading = false;
    }
}
