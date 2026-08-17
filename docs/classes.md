# LokrModAPI — Classes

## `LokrModAPIPlugin` (`LokrModAPIPlugin.cs`)

```csharp
[BepInPlugin(Guid, Name, Version)]
public class LokrModAPIPlugin : BaseUnityPlugin
```

- `internal static ManualLogSource Log` — assembly-wide shared logger.
- `Awake()`: see [`architecture.md`](architecture.md) for the bootstrap sequence.
- `Update()`: calls `audioService.Update()` every frame, and processes a
  `pendingSceneDumps` queue, firing `SceneHierarchyDumper.Dump(...)` for
  any scene whose scheduled due-time has passed.
- `OnSceneLoaded(Scene, LoadSceneMode)`: if
  `Config.DumpSceneHierarchies` is on, schedules a dump 2 seconds in the
  future by adding to `pendingSceneDumps` — **not** via `StartCoroutine`
  (see [`cross-references.md`](cross-references.md)).

## `ModAssetLoader` (`Assets/ModAssetLoader.cs`)

Collapses the repeated `byte[] → Texture2D → LoadImage → Sprite.Create`
pattern (previously copy-pasted in ~8 places) plus WAV loading into one
service.

```csharp
Dictionary<string, Sprite> PackSprites(IEnumerable<KeyValuePair<string, Texture2D>> sourceTextures, float pixelsPerUnit = 100f)
Texture2D LoadTexture(string path, TextureFormat format = TextureFormat.ARGB32)
Sprite LoadSprite(string path, TextureFormat format = TextureFormat.ARGB32)
AudioClip LoadAudioClip(string path)
```

- `PackSprites`: packs textures into one shared atlas via
  `TextureAtlasPacker`, returns a `Sprite` per part, all backed by that
  one atlas. **Always uses `SpriteMeshType.FullRect`**, never the Unity
  default `Tight` — see [`cross-references.md`](cross-references.md) for
  why.
- `LoadTexture`/`LoadSprite`: read a file's raw bytes into a texture (or a
  full-rect `Sprite` wrapping one).
- `LoadAudioClip`: reads a WAV file and converts it via `OpenWavParser`.

## `TextureAtlasPacker` (`Assets/TextureAtlasPacker.cs`)

```csharp
Texture2D Pack(IEnumerable<KeyValuePair<string, Texture2D>> sources, out Dictionary<string, Rect> pixelRects, int padding = 2)
```

A plain "shelf" packer: sorts textures tallest-first, lays left-to-right,
wraps to a new row when one won't fit. Atlas dimensions are rounded up to
powers of two (`Mathf.NextPowerOfTwo`, minimum 64px). Explicitly
documented as "not trying to be space-optimal," just "good enough for
mod-sized part counts." Exists because `ExoSkeletonRenderer` only ever
reads `partSprites[0].texture` as the material for the *entire* mesh, so
any multi-part rig needs every part's texture packed into one shared
atlas with each part's `Sprite` UV rect pointing at its own sub-region.

## `ModAudioService` (`Audio/ModAudioService.cs`)

A direct, non-`MonoBehaviour` port of the old `ModManager.PlaySound`/
`ModdedSound` behavior, driven every frame via `LokrModAPIPlugin.Update()`
calling its internal `Update()`.

```csharp
void PlayClip(AudioClip clip)
void PlaySound(string eventName, string unitId, string modFolder)
internal void Update()
```

- `PlayClip`: fire-and-forget one-shot playback of an already-resolved
  clip — spawns a transient `GameObject`/`AudioSource`, plays once,
  self-destroys after `clip.length + 0.1f` seconds. No caching, no
  mod-folder awareness.
- `PlaySound`: registers/looks up a cached `ModdedSound` (keyed by
  `unitId + eventName`) and marks it for playback on the next tick.
- Private nested `ModdedSound`: on construction, scans
  `<modPath>/Sounds/<unitId>/` for files whose name contains the
  requested sound type, loads each as a WAV, and creates one `AudioSource`
  per matching file — supporting randomized-variant playback (`Play()`
  picks a random source). Its `GameObject` is `DontDestroyOnLoad`.
- Reads volume from `UserSettings.Get("IronhideUserSoundFXVolumeEditorPref", 1f)`
  (see [`cross-references.md`](cross-references.md)).

## `OpenWavParser` (`Audio/OpenWavParser.cs`)

Static, stateless. Explicitly ported **verbatim** from the base game's own
decompiled `ih-modded/Ironhide.Legends/OpenWavParser.cs` — quirks below
mirror the original engine code, not bugs introduced by this port.

```csharp
static bool IsWAVFile(byte[] wavFile)
static AudioClip ByteArrayToAudioClip(byte[] wavFile, string name = "", bool stream = false)
static byte[] AudioClipToByteArray(AudioClip clip, Resolution res = Resolution._16bit)
static AudioClip Combine(AudioClip[] clips)
static AudioClip StereoToMono(AudioClip stereoClip, bool stream = false)
static AudioClip MonoToStereo(AudioClip monoClip, bool stream = false)
enum Resolution { _16bit = 16, _24bit = 24, _32bit = 32 }
```

Only uncompressed PCM is supported — compressed WAV data logs an error
and returns `null`. `Combine()` hardcodes stereo output at 44100 Hz
regardless of input clips' actual channel count/sample rate — worth
knowing before combining non-standard clips.

## `ModConfig` (`Config/ModConfig.cs`)

Replaces `ModManager`'s old `properties.txt` key=value parser with
BepInEx's standard `ConfigFile`-backed configuration.

```csharp
internal ModConfig(ConfigFile config, ManualLogSource log)
```

| Entry | Section/Key | Default | Purpose |
|---|---|---|---|
| `ConfigEntry<bool> DebugMode` | `General/DebugMode` | `false` (or legacy) | Debug logging + in-game debug panel |
| `ConfigEntry<bool> SkipSplashScreen` | `General/SkipSplashScreen` | `false` | Skip intro splash video (see `SplashVideoController_Awake_Patch` below) |
| `ConfigEntry<bool> TakeOverAI` | `General/TakeOverAI` | `false` | Built-in fight-tester AI take-over cheat |
| `ConfigEntry<bool> DumpSceneHierarchies` | `Diagnostics/DumpSceneHierarchies` | `false` | Dev diagnostic, off by default |
| `ConfigEntry<string> SceneDumpPath` | `Diagnostics/SceneDumpPath` | `<Application.dataPath>/SceneDumps` | Where scene dumps are written — defaults next to the game's data folder, works on any OS |

On first run (when the plugin's own `.cfg` doesn't exist yet at
`config.ConfigFilePath`), imports `debug_mode`/`skip_splash_screen`/
`take_over_ai` from a legacy `properties.txt` at
`<Application.dataPath>/Mods/Resources/properties.txt` if present, so
upgrading players don't get their settings silently reset. The legacy
file is left untouched and never re-read after that one-time import.

## `SceneHierarchyDumper` (`Diagnostics/SceneHierarchyDumper.cs`)

Dev-only diagnostic, not used by any runtime patch — writes a scene's full
`GameObject`/`Component` tree to a text file, for finding real field/
hierarchy names instead of guessing from decompiled source alone. Gated
behind `ModAPI.Config.DumpSceneHierarchies`.

```csharp
static void Dump(Scene scene, string outputDirectory)
```

Walks every root `GameObject`, recursively formats name/`activeSelf`/
layer/tag plus components and children (2-space indent per depth), writes
`<outputDirectory>/<sceneName or "unnamed">.txt`. Special-cases detail
output for `RectTransform`, `Text`, `Camera`, and `Canvas`; other
component types just get their type name. Handles `<missing script>`
components (a `null` entry from `GetComponents<Component>()`) explicitly
rather than crashing. `LokrModAPIPlugin.Update()` is responsible for
delaying the actual call ~2 seconds after `sceneLoaded` fires.

## `AttachedData<TKey, TValue>` (`ExtensionData/AttachedData.cs`)

```csharp
public sealed class AttachedData<TKey, TValue> where TKey : class
{
    bool TryGet(TKey key, out TValue value)
    void Set(TKey key, TValue value)
    TValue GetOrAdd(TKey key, Func<TValue> factory)
}
```

A generic, reusable version of the `ConditionalWeakTable`-based "side
table" pattern (previously hand-rolled per-plugin) used to attach data to
a type Harmony/this codebase doesn't own and can't add a field to.
`Set()` has add-or-replace semantics (removes any existing entry before
adding, since `ConditionalWeakTable.Add` alone throws on a duplicate key).
Values are wrapped in `StrongBox<TValue>` internally — required because
`ConditionalWeakTable`'s value type must be a reference type, so this lets
`TValue` be anything (including structs). Because it's backed by
`ConditionalWeakTable`, attached values are garbage-collected together
with their key automatically — no manual cleanup needed, no risk of
leaking data for destroyed Unity objects.

Not exposed via the `ModAPI` facade — other plugins instantiate their own
`private static readonly AttachedData<TKey, TValue>` field directly (see
`LokrCharacterLoader`'s `ExoSkeletonModData` for an example consumer).

## `ModFileSystem` (`Files/ModFileSystem.cs`)

Replaces `ModManager.GetModsFolderPath()` and ~15 duplicated
"for each mod folder, build a path, check `File.Exists`" loops that
existed across the old patches.

```csharp
internal ModFileSystem()   // modsRoot = <Application.dataPath>/Mods
IReadOnlyList<string> GetModFolders()
bool TryFindFile(string category, string relativePath, out string fullPath)
bool TryFindSoundFile(string unitId, string eventName, out string modFolder, out string filePath)
IEnumerable<(string modFolder, string filePath)> EnumerateCategoryFiles(string category, string searchPattern = "*")
IEnumerable<(string modFolder, string itemFolder)> EnumerateCategorySubfolders(string category)
```

- `TryFindFile`: searches every mod folder (in enumeration order) for
  `<modFolder>/<category>/<relativePath>`, returns the first match —
  e.g. `TryFindFile("Portraits", $"{heroId}/{heroId}_MINI.png", out path)`.
  **First-match-wins** — implicit, load-order-dependent override
  behavior.
- `TryFindSoundFile`: exact-match convenience wrapper for
  `<modFolder>/Sounds/<unitId>/<unitId>_<eventName>.wav`, used as a
  pre-check before committing to `ModAPI.Audio.PlaySound` (which then
  does its own broader substring scan internally to pick up randomizable
  variant files).
- `EnumerateCategoryFiles`: every file under `<modFolder>/<category>/`
  across all mod folders — e.g. `EnumerateCategoryFiles("HeroRoster")` for
  roster splicing, `EnumerateCategoryFiles("NewAbilities", "*.txt")` for
  ability injection.
- `EnumerateCategorySubfolders`: every *subfolder* under
  `<modFolder>/<category>/`, for content that's itself a folder of files
  rather than one file — e.g. `Characters/<RigId>/` containing a
  `rig.json` plus one PNG per part (see `LokrCharacterLoader`'s
  `CustomRigLoader`).

## `TextEscaping` (`Serialization/TextEscaping.cs`)

```csharp
public static string JsonEscape(string value)
public static string KvEscape(string value)
```

Added 2026-08-12 fixing pre-redesign audit C-01/M-09 — every hand-built
JSON or KV1 writer across the solution (`LokrCharacterLab`'s
`RLHeroesGenerator`, `RigEditorScene`, `CharacterImporter`,
`CharacterProfileSidecar`) previously interpolated user-typed strings
directly into a `StringBuilder`, so a name/skill/event/part containing a
quote or backslash could corrupt the whole file — for KV1 specifically,
corrupting a shared `TextAsset` other heroes/enemies/abilities are also
spliced into, not just the field that had the bad string.

- `JsonEscape`: standard JSON string escaping (RFC 8259 §7 — backslash,
  quote, the named control-character escapes, `\u00XX` for anything else
  control). Fully lossless here because every reader of this project's
  hand-built JSON is `SimpleJSON`, a real parser.
- `KvEscape`: escapes backslash and quote only, verified against the real
  decompiled `KVLib.KeyValues.PenguinParser` source rather than assumed —
  its quoted-value scan treats a backslash as "skip the next character
  when looking for the closing quote," which is what actually prevents an
  embedded quote from corrupting the file, but it does **not** strip the
  backslash back out when extracting the value (and the base game's own
  `KeyValue.ToString()` writer performs zero escaping at all, since real
  shipped values are assumed never to contain a quote). `KvEscape`'s own
  doc comment has the full account of why this is a deliberate,
  necessarily-not-fully-lossless tradeoff rather than an oversight.
  Newlines are deliberately left unescaped — `PenguinParser` has no
  special handling for them inside a quoted value, so escaping them would
  only break legitimately multi-line text with no corresponding safety
  benefit.

Not routed through the `ModAPI` facade (unlike `Files`/`Assets`/`Audio`/
`Config`) — it's a pure, stateless utility with nothing to initialize at
`Awake()`, the same reasoning `AttachedData<TKey, TValue>` above is a
plain type rather than a facade property.

## `GameInputPoll` (`Input/GameInputPoll.cs`)

Static hotkey registry polled from patched game `Update()` loops (not
`BaseUnityPlugin.Update()` alone — important on Linux/Proton where bare
function keys may not reach Unity).

```csharp
static void Register(string id, KeyBinding binding, Action action)
static void Unregister(string id)
internal static void Tick(string source)
```

- `Register` / `Unregister`: idempotent by `id` — re-registering replaces
  the prior binding.
- `Tick`: called from `InputManagerBehaviourPatches` and
  `KeyboardShortcutListenerPatches` each frame; fires at most one handler
  per frame (first match wins).

Used by `LokrModMenu` (mod menu toggle) and `LokrModAPIPlugin` (Ctrl+Shift+F9
debug dump chord).

## `KeyBinding` (`Input/KeyBinding.cs`)

```csharp
readonly struct KeyBinding(KeyCode key, bool control = false, bool shift = false, bool alt = false)
bool IsDown()
```

Keyboard chord checked via `Input.GetKeyDown` + modifier hold state.
`ToString()` renders human-readable labels (`Ctrl+Shift+F9`).

## `InputManagerBehaviourPatches` / `KeyboardShortcutListenerPatches`

Harmony patches that call `GameInputPoll.Tick(...)` from the game's own input
MonoBehaviours so hotkeys run on the same frames vanilla reads keyboard state.

## `SplashVideoController_Awake_Patch` (`Patches/SplashVideoControllerPatches.cs`)

```csharp
[HarmonyPatch(typeof(SplashVideoController), "Awake")]
internal static class SplashVideoController_Awake_Patch
{
    [HarmonyPrefix] private static bool Prefix()
}
```

The one game-touching patch owned directly by this plugin (its config
lives here too). If `ModAPI.Config.SkipSplashScreen.Value`, calls
`TransitionSceneComponent.TransitionToNextScene("scenes", SceneDB.GetScene("mainScreen"))`
directly (routing through the game's own scene-transition machinery
rather than reimplementing it) and returns `false` to suppress the
original `Awake()` — standard Harmony prefix convention where `false`
skips the patched method. Returns `true` (runs normally) otherwise.
