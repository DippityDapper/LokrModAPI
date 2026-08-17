# LokrModAPI — Conventions

- **Service classes**: `sealed`, named `Mod<Noun>` or `<Domain>Service`
  (`ModAssetLoader`, `ModAudioService`, `ModConfig`, `ModFileSystem`) —
  consistently prefixed to signal "part of the mod API," not a game/
  BepInEx type. Constructors are `internal` (instantiable only within the
  assembly, by `LokrModAPIPlugin`), exposed externally only through the
  read-only-from-outside `ModAPI` facade properties.
- **Static utilities**: stateless, one focused job —
  `OpenWavParser`, `SceneHierarchyDumper`, `TextureAtlasPacker`.
- **Harmony patch naming**: `<TargetType>_<TargetMethod>_Patch`,
  `internal static`, attribute-discovered (`[HarmonyPatch(...)]` +
  `PatchAll()`) rather than manual `Patch(...)` calls — one `Harmony`
  instance per plugin, keyed by that plugin's GUID.
- **Try-pattern methods** follow the standard .NET `bool TryX(..., out y)`
  shape (`TryGet`, `TryFindFile`, `TryFindSoundFile`).
- **Comments cite a planning doc** (`docs/modapi-plan.md`, by section
  number) mapping classes back to a structured, pre-planned refactor from
  the older `ModManager`/`ih-modded` codebase — a `docs/modapi-plan.md`
  exists in the solution root for that context.
