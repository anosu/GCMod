# GCMod Small Refactor Design

## Context

GCMod is a small C# BepInEx IL2CPP mod. The current code is concentrated in a few files:

- `Core/Config.cs` owns configuration entries and parsed color values.
- `Core/InputHandler.cs` handles hotkeys and directly adjusts some patched UI objects.
- `Services/Translation.cs` owns translation state, font loading, and startup translation loading.
- `Services/TranslationCache.cs` owns manifest-based remote/local translation cache logic.
- `Patches/Patch.cs` owns Harmony patches for translation, text style, alpha changes, FPS, and cut-in skipping.

The first refactor should improve maintainability and reduce obvious runtime risk without changing user-facing behavior.

## Goals

- Keep all existing configuration keys, defaults, and hotkey behavior unchanged.
- Reduce repeated logic in `Patch.cs` and `InputHandler.cs`.
- Add small defensive checks around translation/font/cache loading.
- Preserve the current Harmony patch layout unless a local helper makes code clearer.
- Keep the change small enough to verify with a normal build.

## Non-Goals

- Do not redesign the plugin architecture.
- Do not split `Patch.cs` into multiple Harmony patch classes in this round.
- Do not change CDN defaults, translation file layout, cache format, or README behavior.
- Do not add new features or new user-visible settings.

## Recommended Approach

Use a low-risk cleanup pass:

1. Add local helper methods where the same logic is repeated.
2. Improve null handling and JSON/cache failure behavior.
3. Keep public behavior stable.
4. Verify with `dotnet build`.

This gives useful maintenance value without increasing the chance of Harmony binding or mod startup regressions.

## Planned Code Shape

### `InputHandler`

Extract alpha adjustment into shared helpers:

- Detect Alt once through a helper.
- Clamp and round alpha through one helper.
- Apply alpha to one or more `Image` references through one helper.

F6 continues to adjust `NormalAlpha` and updates `NormalFrame` plus `BaseNameFrame`. F7 continues to adjust `CgModeAlpha` and updates `CgModeFrame`.

### `Patch`

Add small helper methods:

- Check whether translation is enabled and the current novel translation is loaded.
- Try to retrieve the current novel dictionary.
- Apply translated text only when the relevant dictionary is present.
- Apply font only when translation is active and `Translation.fontAsset` is loaded.
- Apply image alpha through one reusable method.

Patch method names and Harmony attributes stay in place.

### `Translation`

Keep the static service shape, but make loading clearer:

- Avoid starting duplicate font load coroutines when a load is already in progress.
- Guard against missing or unloaded font assets before applying them.
- Keep startup translation load behavior unchanged.

Public field renames are allowed only if all local references are updated in the same change and the behavior stays identical.

### `TranslationCache`

Add defensive behavior:

- Ensure cache directories exist before writing cache files.
- Catch JSON parse errors when loading local translation files and log them.
- Treat a failed manifest parse as unavailable manifest rather than a successful load.
- Preserve stale-cache fallback behavior.

## Error Handling

Network and cache failures should continue to degrade gracefully:

- Remote manifest failure falls back to local manifest when available.
- Translation file fetch failure falls back to local cache when available.
- Missing cache after remote failure reports a toast error as it does today.
- Invalid local JSON should log an error and return failure instead of throwing through the caller.

## Testing

Primary verification:

- Run `dotnet build` before or after the change depending on dependency availability.
- Run `dotnet build` after the change.

If local game DLLs or the `Utility` dependency are unavailable, document the exact build failure and still review changed files for compile-time issues visible from source.

## Acceptance Criteria

- Existing config keys and hotkeys remain unchanged.
- No broad architecture rewrite is introduced.
- Translation/cache/font failure paths are at least as graceful as before.
- `Patch.cs` and `InputHandler.cs` have less duplicated logic.
- Build status is reported with command output summary.
