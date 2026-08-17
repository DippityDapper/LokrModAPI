# LokrModAPI — Overview

The foundational shared-utility plugin every other plugin in this solution
depends on. Replaces scattered, copy-pasted functionality from the older
`ModManager`-era modding approach (asset loading, mod-folder file lookup,
config parsing, audio playback) with one well-organized, reusable API
surface — exposed via the static `ModAPI` facade — so other plugins never
reimplement texture/sprite loading, WAV parsing, mod-folder scanning,
config binding, "attach data to a type Harmony doesn't own" patterns, or
(since 2026-08-12) escaping a hand-built JSON/KV1 string safely.
Also owns the one direct game patch that logically belongs with its own
config: skipping the splash video.

## In this folder

- [`layout.md`](layout.md) — file structure and namespace organization
- [`architecture.md`](architecture.md) — the `ModAPI` facade pattern
- [`classes.md`](classes.md) — every service/utility class and the splash-skip patch
- [`conventions.md`](conventions.md) — naming and structural patterns
- [`cross-references.md`](cross-references.md) — base-game/Unity behavior this code depends on or works around

## Key architectural changes

- **BepInEx `ConfigFile` integration**: Config is now backed by BepInEx's native configuration system (`BepInEx/config/com.lokrmodding.lokrmodapi.cfg`), replacing the old hand-rolled `properties.txt` parser. See [`architecture.md`](architecture.md) for the one-time migration from `properties.txt` (automatic on first run, no manual steps required).
- **Stateless service architecture**: All four `ModAPI` sub-services (`Files`, `Assets`, `Audio`, `Config`) are instantiated once in `LokrModAPIPlugin.Awake()` and exposed through the static facade, so consumers never instantiate or manage their own instances.
- **Guaranteed initialization order**: BepInEx's `[BepInDependency]` system ensures `LokrModAPI` is fully initialized before any consuming plugin's own `Awake()` runs — safe to call `ModAPI.*` from any other plugin's startup code.

## Plugin metadata

`LokrModAPIPlugin.cs`: `Guid = "com.lokrmodding.lokrmodapi"`,
`Name = "LoKR Mod API"`, `Version = "1.0.0"`. No `[BepInDependency]` — this
plugin has no dependencies of its own; every other plugin depends on it
instead (via `[BepInDependency(LokrModAPIPlugin.Guid)]`, referencing these
`public const string` fields directly).
