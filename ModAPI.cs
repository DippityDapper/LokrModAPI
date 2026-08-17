namespace LokrModAPI
{
	/// <summary>Static facade exposing Files, Assets, Audio, and Config services to every other plugin.</summary>
	/// <remarks>
	/// The one thing other LoKR plugins reference. Each sub-API is a small, independently-evolvable
	/// service (docs/modapi-plan.md §4), wired up once by LokrModAPIPlugin.Awake() before any other
	/// plugin's Awake() runs (guaranteed by BepInEx's [BepInDependency] load ordering).
	/// </remarks>
	public static class ModAPI
	{
		/// <summary>Mod folder discovery and file resolution across all installed mods.</summary>
		public static Files.ModFileSystem Files { get; internal set; }
		/// <summary>Loads textures, sprites, and audio clips from mod folders.</summary>
		public static Assets.ModAssetLoader Assets { get; internal set; }
		/// <summary>Sound playback service supporting one-shot clips and cached, randomized mod sounds.</summary>
		public static Audio.ModAudioService Audio { get; internal set; }
		/// <summary>BepInEx ConfigFile-backed settings, replacing the legacy properties.txt parser.</summary>
		public static Config.ModConfig Config { get; internal set; }
	}
}
