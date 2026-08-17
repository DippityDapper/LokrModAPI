using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LokrModAPI.Files
{
	/// <summary>Mod folder discovery and file resolution across all installed mods.</summary>
	/// <remarks>
	/// Replaces ModManager.GetModsFolderPath() and the ~15 duplicated
	/// "for each mod folder, build a path, check File.Exists" loops across the old patches
	/// (docs/modapi-plan.md §4.1).
	/// </remarks>
	public sealed class ModFileSystem
	{
		private readonly string modsRoot;

		/// <summary>Resolves the game's Mods root folder path under Application.dataPath.</summary>
		internal ModFileSystem()
		{
			modsRoot = Path.Combine(Application.dataPath, "Mods");
		}

		/// <summary>Every installed mod's folder path, under the game's Mods directory.</summary>
		public IReadOnlyList<string> GetModFolders()
		{
			return ModPathLookup.GetModFolders(modsRoot);
		}

		/// <summary>Finds the first installed mod providing this exact relative file in a category folder.</summary>
		/// <remarks>e.g. TryFindFile("Portraits", $"{heroId}/{heroId}_MINI.png", out path)</remarks>
		public bool TryFindFile(string category, string relativePath, out string fullPath)
		{
			return ModPathLookup.TryFindFile(modsRoot, category, relativePath, out fullPath);
		}

		/// <summary>Checks whether a mod overrides this exact sound event, before the broader randomized-variant scan.</summary>
		/// <remarks>
		/// Exact-match convenience wrapper for the "does a mod override this specific sound event"
		/// pre-check every sound hook does before calling ModAPI.Audio.PlaySound. ModAPI.Audio does
		/// its own broader substring scan internally once a redirect is committed to, to pick up
		/// multiple randomizable variant files -- see ModAudioService.
		/// </remarks>
		public bool TryFindSoundFile(string unitId, string eventName, out string modFolder, out string filePath)
		{
			IReadOnlyList<string> modFolders = GetModFolders();
			for (int i = 0; i < modFolders.Count; i++)
			{
				string candidate = Path.Combine(modFolders[i], "Sounds", unitId, unitId + "_" + eventName + ".wav");
				if (File.Exists(candidate))
				{
					modFolder = modFolders[i];
					filePath = candidate;
					return true;
				}
			}
			modFolder = null;
			filePath = null;
			return false;
		}

		/// <summary>Every file any installed mod drops in a category folder, across all mods.</summary>
		/// <remarks>
		/// e.g. EnumerateCategoryFiles("HeroRoster") for legend_*/companion_* splicing,
		/// EnumerateCategoryFiles("NewAbilities", "*.txt") for ability injection.
		/// </remarks>
		public IEnumerable<(string modFolder, string filePath)> EnumerateCategoryFiles(string category, string searchPattern = "*")
		{
			IReadOnlyList<string> modFolders = GetModFolders();
			for (int i = 0; i < modFolders.Count; i++)
			{
				string categoryFolder = Path.Combine(modFolders[i], category);
				if (!Directory.Exists(categoryFolder))
				{
					continue;
				}
				string[] files = Directory.GetFiles(categoryFolder, searchPattern);
				for (int j = 0; j < files.Length; j++)
				{
					yield return (modFolders[i], files[j]);
				}
			}
		}

		/// <summary>Every mod-provided subfolder in a category, for content that's a folder rather than a single file.</summary>
		/// <remarks>e.g. CharacterRigs/&lt;RigId&gt;/ containing a rig.json plus one PNG per part.</remarks>
		public IEnumerable<(string modFolder, string itemFolder)> EnumerateCategorySubfolders(string category)
		{
			IReadOnlyList<string> modFolders = GetModFolders();
			for (int i = 0; i < modFolders.Count; i++)
			{
				string categoryFolder = Path.Combine(modFolders[i], category);
				if (!Directory.Exists(categoryFolder))
				{
					continue;
				}
				string[] subfolders = Directory.GetDirectories(categoryFolder);
				for (int j = 0; j < subfolders.Length; j++)
				{
					yield return (modFolders[i], subfolders[j]);
				}
			}
		}
	}
}
