# LokrModAPI — Cross-references

- **`ExoSkeletonRenderer`'s fixed 4-vertex/6-index-per-part mesh
  assumption** drives both `SpriteMeshType.FullRect` (never `Tight`) in
  `ModAssetLoader`, and the single-shared-atlas requirement in
  `TextureAtlasPacker` (the renderer only ever reads
  `partSprites[0].texture`). `Tight` mode traces the alpha-channel
  silhouette and produces a variable vertex/triangle count for anything
  with transparent margins (i.e. essentially all real character art),
  which crashes the renderer with an `IndexOutOfRangeException` with no
  useful stack trace into game code. See
  `../../LokrLab/docs/architecture.md` for where this same
  constraint resurfaces throughout the Animator workstation, and
  `../../LokrCharacterLoader/docs/custom-rig-loader.md` for the same
  constraint in the runtime rig loader.
- **`UserSettings.Get("IronhideUserSoundFXVolumeEditorPref", 1f)`** is the
  base game's own SFX-volume preference key (`Ironhide.Legends.Services.
  Persistence`), reused rather than duplicated.
- **`SceneManager.sceneLoaded` + `StartCoroutine` incompatibility**: a
  `NullReferenceException` from Unity's native implementation when
  starting a coroutine from inside a `sceneLoaded` handler — root cause
  unconfirmed ("possibly a Mono/BepInEx-specific timing issue with the
  coroutine scheduler mid scene-transition"), worked around by polling
  from `Update()` instead.
- **`Stage.TakeOverAICheat`/`CheatDebugController.DEBUG_PANEL_ENABLED`**
  are pre-existing base-game debug/cheat static fields, repurposed for
  modder convenience via config rather than new functionality.
- **`SplashVideoController`/`TransitionSceneComponent`/`SceneDB`** are
  base-game types under `Ironhide.Legends.View.Screens.Transition`/
  `Ironhide.Legends` — the splash-skip patch calls the game's own
  transition machinery rather than reimplementing it.
