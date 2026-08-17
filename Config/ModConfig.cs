using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace LokrModAPI.Config
{
	/// <summary>BepInEx ConfigFile-backed settings, replacing the legacy properties.txt parser.</summary>
	/// <remarks>
	/// Replaces ModManager's properties.txt parser (docs/modapi-plan.md §4.4). Backed by
	/// BepInEx's own ConfigFile so players get the standard .cfg UX (hand-editable, works with
	/// BepInEx.ConfigurationManager) instead of a hand-rolled key=value parser.
	/// </remarks>
	public sealed class ModConfig
	{
		/// <summary>Enables debug logging and the in-game debug panel.</summary>
		public ConfigEntry<bool> DebugMode { get; }
		/// <summary>Skip the intro splash video and go straight to the main menu.</summary>
		public ConfigEntry<bool> SkipSplashScreen { get; }
		/// <summary>Enables the built-in fight-tester AI take-over cheat.</summary>
		public ConfigEntry<bool> TakeOverAI { get; }
		/// <summary>When enabled, writes a full GameObject/Component hierarchy dump a couple seconds after every scene load. Off by default.</summary>
		public ConfigEntry<bool> DumpSceneHierarchies { get; }
		/// <summary>Folder scene hierarchy dumps are written to when DumpSceneHierarchies is enabled.</summary>
		public ConfigEntry<string> SceneDumpPath { get; }

		/// <summary>Binds every config entry, importing legacy properties.txt values on first run if present.</summary>
		/// <remarks>
		/// On first run for this plugin (its .cfg doesn't exist yet), imports the legacy
		/// properties.txt the official community pack already ships, so upgrading players don't
		/// have their settings silently reset to defaults. properties.txt itself is left on disk
		/// untouched -- just never read again after this.
		/// </remarks>
		internal ModConfig(ConfigFile config, ManualLogSource log)
		{
			Dictionary<string, string> legacy = null;
			if (!File.Exists(config.ConfigFilePath))
			{
				string legacyPath = Path.Combine(Application.dataPath, "Mods", "Resources", "properties.txt");
				if (File.Exists(legacyPath))
				{
					legacy = ParseLegacyProperties(legacyPath);
					log.LogInfo("Imported settings from properties.txt — edit " + config.ConfigFilePath + " from now on.");
				}
			}

			DebugMode = config.Bind("General", "DebugMode", LegacyBool(legacy, "debug_mode", false),
				"Enables debug logging and the in-game debug panel.");
			SkipSplashScreen = config.Bind("General", "SkipSplashScreen", LegacyBool(legacy, "skip_splash_screen", false),
				"Skip the intro splash video and go straight to the main menu.");
			TakeOverAI = config.Bind("General", "TakeOverAI", LegacyBool(legacy, "take_over_ai", false),
				"Enables the built-in fight-tester AI take-over cheat.");
			DumpSceneHierarchies = config.Bind("Diagnostics", "DumpSceneHierarchies", false,
				"When enabled, writes a full GameObject/Component hierarchy dump to SceneDumpPath a couple seconds after every scene load. Off by default — dev diagnostic only.");
			SceneDumpPath = config.Bind("Diagnostics", "SceneDumpPath", Path.Combine(Application.dataPath, "SceneDumps"),
				"Folder scene hierarchy dumps are written to when DumpSceneHierarchies is enabled.");
		}

		/// <summary>Reads a boolean from the imported legacy properties dictionary, falling back if absent or unparsable.</summary>
		private static bool LegacyBool(Dictionary<string, string> legacy, string key, bool fallback)
		{
			if (legacy != null && legacy.TryGetValue(key, out string value) && bool.TryParse(value, out bool parsed))
			{
				return parsed;
			}
			return fallback;
		}

		/// <summary>Parses a properties.txt file's key=value lines, ignoring blanks and #-comments.</summary>
		private static Dictionary<string, string> ParseLegacyProperties(string filePath)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] lines = File.ReadAllLines(filePath);
			for (int i = 0; i < lines.Length; i++)
			{
				string text = lines[i].Trim();
				if (!string.IsNullOrEmpty(text) && !text.StartsWith("#"))
				{
					string[] parts = text.Split(new char[] { '=' }, 2);
					if (parts.Length == 2)
					{
						dictionary[parts[0].Trim()] = parts[1].Trim();
					}
				}
			}
			return dictionary;
		}
	}
}
