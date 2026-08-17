using System;
using System.Collections.Generic;
using System.IO;

namespace LokrModAPI.Files
{
	/// <summary>Unity-free mod-folder path helpers that <see cref="ModFileSystem"/> uses against the live Mods root.</summary>
	/// <remarks>
	/// Extracted so xUnit can run the same lookup against a temp tree without constructing
	/// <see cref="ModFileSystem"/> (that type reads <c>Application.dataPath</c>).
	/// </remarks>
	public static class ModPathLookup
	{
		/// <summary>Every immediate subdirectory of <paramref name="modsRoot"/>, or empty when the root is missing.</summary>
		public static IReadOnlyList<string> GetModFolders(string modsRoot)
		{
			if (string.IsNullOrEmpty(modsRoot) || !Directory.Exists(modsRoot))
			{
				return Array.Empty<string>();
			}

			return Directory.GetDirectories(modsRoot);
		}

		/// <summary>Finds the first mod folder that contains <paramref name="category"/>/<paramref name="relativePath"/>.</summary>
		public static bool TryFindFile(string modsRoot, string category, string relativePath, out string fullPath)
		{
			IReadOnlyList<string> modFolders = GetModFolders(modsRoot);
			for (int i = 0; i < modFolders.Count; i++)
			{
				string candidate = Path.Combine(modFolders[i], category, relativePath);
				if (File.Exists(candidate))
				{
					fullPath = candidate;
					return true;
				}
			}

			fullPath = null;
			return false;
		}
	}
}
