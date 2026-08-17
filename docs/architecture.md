# LokrModAPI — Architecture

## The `ModAPI` facade

```csharp
public static class ModAPI
{
    public static Files.ModFileSystem Files { get; internal set; }
    public static Assets.ModAssetLoader Assets { get; internal set; }
    public static Audio.ModAudioService Audio { get; internal set; }
    public static Config.ModConfig Config { get; internal set; }
}
```

A flat static facade, not nested namespaces — each property's *type*
lives in its own sub-namespace, but the properties themselves sit flat on
`ModAPI`, so consuming code writes `ModAPI.Audio.PlaySound(...)`,
`ModAPI.Files.TryFindFile(...)`, `ModAPI.Assets.LoadSprite(...)`,
`ModAPI.Config.DebugMode.Value`. This is **the one thing other plugins
reference** — wired up once by `LokrModAPIPlugin.Awake()` before any other
plugin's `Awake()` runs (guaranteed by BepInEx's `[BepInDependency]` load
ordering on the *consuming* side).

The four properties have `internal set` — `LokrModAPIPlugin.Awake()` is
effectively the only writer in the whole solution; other plugins can only
read them.

Two utilities are deliberately **not** on the facade:
`SceneHierarchyDumper` (static/stateless, called directly by type) and
`AttachedData<TKey,TValue>` (generic — other plugins instantiate their own
private instance rather than sharing one through `ModAPI`). See
[`classes.md`](classes.md) for both.

## Bootstrap order

`LokrModAPIPlugin.Awake()`:

1. Instantiates and assigns all four `ModAPI` sub-services (`Files`,
   `Assets`, `Audio`, `Config`).
2. Applies `Stage.TakeOverAICheat` and
   `CheatDebugController.DEBUG_PANEL_ENABLED` one-time flags from config
   (mirroring the old `ModManager.OnGameStart()`).
3. Subscribes `OnSceneLoaded` to `SceneManager.sceneLoaded`.
4. `new Harmony(Guid).PatchAll()`.

Because every other plugin in the solution declares
`[BepInDependency(LokrModAPIPlugin.Guid)]`, BepInEx guarantees this
sequence completes before any other plugin's own `Awake()` runs — so
`ModAPI.Files`/`Assets`/`Audio`/`Config` are always safe to use from any
other plugin's `Awake()` onward.

See [`cross-references.md`](cross-references.md) for the
`SceneManager.sceneLoaded`/`StartCoroutine` incompatibility that shapes
`Update()`'s scene-dump-scheduling design.
