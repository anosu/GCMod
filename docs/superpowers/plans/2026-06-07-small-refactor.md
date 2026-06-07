# GCMod Small Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Perform a low-risk cleanup of GCMod's input, translation, cache, and patch code without changing user-facing behavior.

**Architecture:** Keep the existing static service and Harmony patch structure. Extract repeated local logic into small helpers, add defensive cache/font handling, and verify behavior through builds and focused source review.

**Tech Stack:** C# `net6.0`, BepInEx Unity IL2CPP, Harmony, UnityEngine, TMPro, `System.Text.Json`, PowerShell, `dotnet build`.

---

## File Structure

- Modify `GCMod/Core/InputHandler.cs`: shared hotkey alpha adjustment helpers.
- Modify `GCMod/Patches/Patch.cs`: local helper methods for translation availability, font application, translated lookup, and alpha application.
- Modify `GCMod/Services/Translation.cs`: clearer font load lifecycle and safer startup loading.
- Modify `GCMod/Services/TranslationCache.cs`: safer JSON/cache/manifest handling.
- Do not modify config keys, defaults, README behavior, project references, or release packaging.

## Task 1: Establish Build Baseline

**Files:**
- Read: `GCMod/GCMod.csproj`
- No source modifications.

- [ ] **Step 1: Check working tree**

Run:

```powershell
git status --short
```

Expected: only the committed docs work should be present, or any uncommitted files should be understood before editing.

- [ ] **Step 2: Run baseline build**

Run:

```powershell
dotnet build GCMod.sln
```

Expected: either `Build succeeded` or a dependency-related failure caused by local game/Utility references. Record the exact failure if the build cannot run locally.

- [ ] **Step 3: Inspect current warnings/errors**

If build fails, confirm whether the failure references paths from `GCMod/GCMod.csproj`, especially:

```xml
<GameDir>E:\Games\DMM GAMES\girlscreation_r</GameDir>
<HintPath>..\..\Utility\Utility\bin\Release\netstandard2.1\Utility.dll</HintPath>
```

Expected: no source edits in this task.

## Task 2: Refactor `InputHandler` Alpha Hotkeys

**Files:**
- Modify: `GCMod/Core/InputHandler.cs`

- [ ] **Step 1: Add helper methods without changing behavior**

Add these private helpers inside `InputHandler`:

```csharp
private static bool IsAltPressed()
{
    return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
}

private static float GetAlphaDelta()
{
    return IsAltPressed() ? 0.1f : -0.1f;
}

private static float ClampAlpha(float value)
{
    return Mathf.Clamp(Mathf.Round(value * 10f) / 10f, 0f, 1f);
}

private static void ApplyAlpha(float alpha, params UnityEngine.UI.Image[] images)
{
    foreach (var image in images)
    {
        if (image == null)
            continue;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
```

- [ ] **Step 2: Replace F6 logic with helper calls**

Change the F6 block to:

```csharp
if (Input.GetKeyDown(KeyCode.F6))
{
    Config.NormalAlpha.Value = ClampAlpha(Config.NormalAlpha.Value + GetAlphaDelta());
    ApplyAlpha(Config.NormalAlpha.Value, Patch.NormalFrame, Patch.BaseNameFrame);
}
```

- [ ] **Step 3: Replace F7 logic with helper calls**

Change the F7 block to:

```csharp
if (Input.GetKeyDown(KeyCode.F7))
{
    Config.CgModeAlpha.Value = ClampAlpha(Config.CgModeAlpha.Value + GetAlphaDelta());
    ApplyAlpha(Config.CgModeAlpha.Value, Patch.CgModeFrame);
}
```

- [ ] **Step 4: Build after input refactor**

Run:

```powershell
dotnet build GCMod.sln
```

Expected: same build status category as Task 1, with no new syntax/type errors from `InputHandler.cs`.

- [ ] **Step 5: Commit input refactor**

Run:

```powershell
git add GCMod\Core\InputHandler.cs
git commit -m "refactor: simplify alpha hotkey handling"
```

Expected: commit succeeds.

## Task 3: Harden `TranslationCache`

**Files:**
- Modify: `GCMod/Services/TranslationCache.cs`

- [ ] **Step 1: Add safe dictionary loading**

Replace `LoadFromFile` with:

```csharp
private static Dictionary<string, string> LoadFromFile(string path)
{
    try
    {
        var json = File.ReadAllText(path, Utf8);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }
    catch (Exception e)
    {
        Plugin.Log.LogError($"Failed to load translation cache {path}: {e.Message}");
        return null;
    }
}
```

- [ ] **Step 2: Ensure cache directory before writing**

Replace `SaveToFile` with:

```csharp
private static void SaveToFile(string path, Dictionary<string, string> data)
{
    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory))
        Directory.CreateDirectory(directory);

    var json = JsonSerializer.Serialize(data, HashJsonOptions);
    File.WriteAllText(path, json, Utf8);
}
```

- [ ] **Step 3: Treat invalid remote manifest as failed**

Inside `FetchManifestAsync`, after deserializing the manifest, add a null check before writing the file:

```csharp
_manifest = JsonSerializer.Deserialize<ManifestData>(json);
if (_manifest == null)
{
    Plugin.Log.LogWarning("Remote manifest parse returned null");
}
else
{
    await File.WriteAllTextAsync(path, json, Utf8);
    Plugin.Log.LogInfo($"Manifest loaded ({_language}). Hash: {_manifest.Hash}");
    return;
}
```

The existing fallback to `TryLoadLocalManifest(path)` should still execute if parsing returns null.

- [ ] **Step 4: Make local manifest parse defensive**

In `TryLoadLocalManifest`, after deserializing, only log success and toast when `_manifest != null`; otherwise log a warning and leave manifest unavailable:

```csharp
_manifest = JsonSerializer.Deserialize<ManifestData>(json);
if (_manifest != null)
{
    Plugin.Log.LogInfo($"Loaded cached manifest from local ({_language}). Hash: {_manifest.Hash}");
    Toast.Warn("翻译服务", "无法连接远程，使用本地翻译清单");
}
else
{
    Plugin.Log.LogWarning("Cached manifest parse returned null");
}
```

- [ ] **Step 5: Build after cache hardening**

Run:

```powershell
dotnet build GCMod.sln
```

Expected: same build status category as Task 1, with no new syntax/type errors from `TranslationCache.cs`.

- [ ] **Step 6: Commit cache hardening**

Run:

```powershell
git add GCMod\Services\TranslationCache.cs
git commit -m "fix: harden translation cache loading"
```

Expected: commit succeeds.

## Task 4: Refactor Translation Font Loading

**Files:**
- Modify: `GCMod/Services/Translation.cs`

- [ ] **Step 1: Add font loading state**

Add this static field near the other font fields:

```csharp
private static bool fontAssetLoading;
```

- [ ] **Step 2: Add a safe coroutine starter**

Add this method to `Translation`:

```csharp
public static void EnsureFontAssetLoading()
{
    if (fontAsset != null || fontAssetLoading || !Config.Translation.Value)
        return;

    Plugin.Instance.StartCoroutine(LoadFontAsset());
}
```

- [ ] **Step 3: Use safe starter during initialization**

Change:

```csharp
Plugin.Instance.StartCoroutine(LoadFontAsset());
```

to:

```csharp
EnsureFontAssetLoading();
```

- [ ] **Step 4: Guard coroutine state**

At the start of `LoadFontAsset`, after the early return check, set loading state:

```csharp
fontAssetLoading = true;
```

Wrap the rest of the coroutine body in a `try`/`finally`-style pattern available to Unity coroutines by setting `fontAssetLoading = false;` before each terminal `yield break` and at the end of the method. The final shape must ensure missing bundle, missing asset, and successful asset load all clear `fontAssetLoading`.

- [ ] **Step 5: Build after translation refactor**

Run:

```powershell
dotnet build GCMod.sln
```

Expected: same build status category as Task 1, with no new syntax/type errors from `Translation.cs`.

- [ ] **Step 6: Commit translation refactor**

Run:

```powershell
git add GCMod\Services\Translation.cs
git commit -m "refactor: guard font asset loading"
```

Expected: commit succeeds.

## Task 5: Refactor Patch Helpers

**Files:**
- Modify: `GCMod/Patches/Patch.cs`

- [ ] **Step 1: Add helper methods**

Add these private helpers near the top of `Patch` after `Initialize`:

```csharp
private static bool TryGetCurrentNovel(out Dictionary<string, string> translation)
{
    translation = null;
    return Config.Translation.Value && Translation.novels.TryGetValue(novelId, out translation);
}

private static bool HasTranslationFont()
{
    return Config.Translation.Value && Translation.fontAsset != null;
}

private static void ApplyTranslationFont(TextMeshProUGUI text)
{
    if (text != null && HasTranslationFont())
        text.font = Translation.fontAsset;
}

private static void ApplyImageAlpha(Image image, float alpha)
{
    if (image == null)
        return;

    Color color = image.color;
    color.a = alpha;
    image.color = color;
}
```

- [ ] **Step 2: Replace repeated current-novel checks**

For title and text dictionary lookup, replace `Translation.novels.ContainsKey(novelId)` plus indexer usage with `TryGetCurrentNovel(out var translation)`:

```csharp
if (TryGetCurrentNovel(out var translation) &&
    translation.TryGetValue(__instance._TitleMain.text, out string title))
{
    __instance._TitleMain.text = title;
}
```

and:

```csharp
if (TryGetCurrentNovel(out var translation) &&
    translation.TryGetValue(message, out string text))
{
    message = text;
}
```

- [ ] **Step 3: Replace direct font assignment**

Use `ApplyTranslationFont(...)` in title, name, and text font patch methods:

```csharp
if (TryGetCurrentNovel(out _))
    ApplyTranslationFont(__instance._TitleMain);
```

```csharp
if (TryGetCurrentNovel(out _))
    ApplyTranslationFont(__instance.MessageName);
```

```csharp
if (TryGetCurrentNovel(out _))
    ApplyTranslationFont(text);
```

- [ ] **Step 4: Replace repeated image alpha code**

In `SaveTextBackgound`, replace manual color mutation with:

```csharp
ApplyImageAlpha(image, Config.NormalAlpha.Value);
NormalFrame = image;
```

for `Normal`, with:

```csharp
ApplyImageAlpha(image, Config.CgModeAlpha.Value);
CgModeFrame = image;
```

for `MessageWindow`, and with:

```csharp
ApplyImageAlpha(image, Config.NormalAlpha.Value);
BaseNameFrame = image;
```

for `BaseName`.

- [ ] **Step 5: Replace duplicate font load coroutine calls**

Change calls like:

```csharp
Plugin.Instance.StartCoroutine(Translation.LoadFontAsset());
```

to:

```csharp
Translation.EnsureFontAssetLoading();
```

- [ ] **Step 6: Build after patch refactor**

Run:

```powershell
dotnet build GCMod.sln
```

Expected: same build status category as Task 1, with no new syntax/type errors from `Patch.cs`.

- [ ] **Step 7: Commit patch refactor**

Run:

```powershell
git add GCMod\Patches\Patch.cs
git commit -m "refactor: simplify patch translation helpers"
```

Expected: commit succeeds.

## Task 6: Final Verification

**Files:**
- Read: all modified source files.

- [ ] **Step 1: Run final build**

Run:

```powershell
dotnet build GCMod.sln
```

Expected: `Build succeeded`, or the same dependency-related failure recorded in Task 1 with no new source errors.

- [ ] **Step 2: Review source diff**

Run:

```powershell
git diff HEAD~4..HEAD -- GCMod\Core\InputHandler.cs GCMod\Services\TranslationCache.cs GCMod\Services\Translation.cs GCMod\Patches\Patch.cs
```

Expected: diff only contains the planned helper extraction and defensive handling.

- [ ] **Step 3: Check final status**

Run:

```powershell
git status --short
```

Expected: clean working tree, or only intentional docs/plan changes if the plan was not committed.

