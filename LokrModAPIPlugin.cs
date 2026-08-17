using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Ironhide.Legends;
using Ironhide.Legends.Model.Game;
using LokrModAPI.Assets;
using LokrModAPI.Audio;
using LokrModAPI.Config;
using LokrModAPI.Diagnostics;
using LokrModAPI.Files;
using LokrModAPI.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LokrModAPI
{
	/// <summary>Plugin entry point that wires up the ModAPI facade and drives its per-frame diagnostics.</summary>
	[BepInPlugin(Guid, Name, Version)]
	public class LokrModAPIPlugin : BaseUnityPlugin
	{
		/// <summary>This plugin's BepInEx GUID.</summary>
		public const string Guid = "com.lokrmodding.lokrmodapi";
		/// <summary>This plugin's display name.</summary>
		public const string Name = "LoKR Mod API";
		/// <summary>This plugin's version string.</summary>
		public const string Version = "1.0.0";

		/// <summary>This plugin's shared BepInEx log source, set once in Awake().</summary>
		internal static ManualLogSource Log;

		private Harmony harmony;
		private ModAudioService audioService;
		private readonly List<(Scene scene, float dueTime)> pendingSceneDumps = new List<(Scene, float)>();

		/// <summary>Wires up the ModAPI facade's sub-services and applies boot-time config flags.</summary>
		/// <remarks>One-time flags applied at boot, same as ModManager.OnGameStart() did.</remarks>
		private void Awake()
		{
			Log = base.Logger;

			ModAPI.Files = new ModFileSystem();
			ModAPI.Assets = new ModAssetLoader();
			audioService = new ModAudioService();
			ModAPI.Audio = audioService;
			ModAPI.Config = new ModConfig(base.Config, Log);

			if (ModAPI.Config.TakeOverAI.Value)
			{
				Stage.TakeOverAICheat = true;
			}
			if (ModAPI.Config.DebugMode.Value)
			{
				CheatDebugController.DEBUG_PANEL_ENABLED = true;
			}

			SceneManager.sceneLoaded += OnSceneLoaded;

			harmony = new Harmony(Guid);
			harmony.PatchAll();

			if (ModAPI.Config.DumpSceneHierarchies.Value)
			{
				GameInputPoll.Register(
					"DumpSceneHierarchy",
					new KeyBinding(KeyCode.F9, control: true, shift: true),
					DumpActiveSceneNow);
			}

			Log.LogInfo(string.Format(
				"{0} v{1} loaded — {2} method(s) patched.",
				Name, Version, harmony.GetPatchedMethods().Count()));
			if (ModAPI.Config.DumpSceneHierarchies.Value)
			{
				Log.LogInfo("Scene hierarchy dump hotkey: Ctrl+Shift+F9 (bare F9 often blocked on Linux/Proton).");
			}
		}

		private static void DumpActiveSceneNow()
		{
			SceneHierarchyDumper.Dump(SceneManager.GetActiveScene(), ModAPI.Config.SceneDumpPath.Value);
		}

		/// <summary>Drives the audio service's per-frame bookkeeping and fires due scene dumps.</summary>
		private void Update()
		{
			audioService.Update();

			for (int i = pendingSceneDumps.Count - 1; i >= 0; i--)
			{
				if (Time.time >= pendingSceneDumps[i].dueTime)
				{
					SceneHierarchyDumper.Dump(pendingSceneDumps[i].scene, ModAPI.Config.SceneDumpPath.Value);
					pendingSceneDumps.RemoveAt(i);
				}
			}
		}

		/// <summary>Logs every scene load unconditionally, and schedules a hierarchy dump 2 seconds later if DumpSceneHierarchies is enabled.</summary>
		/// <remarks>
		/// The log line is unconditional (not gated behind DumpSceneHierarchies) -- the single most
		/// useful line for diagnosing whether/when scene loads are firing at all and what they're
		/// named, mirroring the exact pattern an earlier prototype
		/// (lokr-modding/bepin/ModAPI/ModAPI.cs's own OnSceneLoaded) already proved out.
		///
		/// The dump itself is scheduled via Update() rather than a coroutine: StartCoroutine throws
		/// a NullReferenceException from inside Unity's own native implementation when called from
		/// this sceneLoaded callback (reproducible, cause unclear -- possibly a Mono/BepInEx-specific
		/// timing issue with the coroutine scheduler mid scene-transition). Driving the delay off
		/// Update() instead sidesteps it entirely and needs no coroutine machinery anyway.
		/// </remarks>
		private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Log.LogInfo("Scene loaded: " + scene.name + " (mode=" + mode + ")");

			if (ModAPI.Config.DumpSceneHierarchies.Value)
			{
				pendingSceneDumps.Add((scene, Time.time + 2f));
			}
		}
	}
}
