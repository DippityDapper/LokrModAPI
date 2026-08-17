# LokrModAPI — Layout

```
LokrModAPI/
├── LokrModAPIPlugin.cs
├── ModAPI.cs                          (the facade)
├── Assets/
│   ├── ModAssetLoader.cs
│   └── TextureAtlasPacker.cs
├── Audio/
│   ├── ModAudioService.cs
│   └── OpenWavParser.cs
├── Config/
│   └── ModConfig.cs
├── Diagnostics/
│   └── SceneHierarchyDumper.cs
├── ExtensionData/
│   └── AttachedData.cs
├── Files/
│   ├── ModFileSystem.cs
│   └── ModPathLookup.cs
├── Input/
│   ├── GameInputPoll.cs
│   └── KeyBinding.cs
├── Serialization/
│   └── TextEscaping.cs
└── Patches/
    ├── SplashVideoControllerPatches.cs
    ├── InputManagerBehaviourPatches.cs
    └── KeyboardShortcutListenerPatches.cs
```

Namespace = folder, one-to-one: `LokrModAPI.Assets`, `LokrModAPI.Audio`,
`LokrModAPI.Config`, `LokrModAPI.Diagnostics`, `LokrModAPI.ExtensionData`,
`LokrModAPI.Files`, `LokrModAPI.Input`, `LokrModAPI.Serialization`,
`LokrModAPI.Patches`, plus the root `LokrModAPI` namespace for the plugin
class and the facade. `Serialization` deliberately isn't named `Text` —
that name collides with `UnityEngine.UI.Text`, which several consumer
plugins reference unqualified in the same enclosing namespace chain (a
real build break hit and fixed while adding `TextEscaping.cs`, see
`classes.md`).
