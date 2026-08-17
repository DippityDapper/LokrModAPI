using System.Collections.Generic;
using System.IO;
using Ironhide.Legends.Services.Persistence;
using UnityEngine;

namespace LokrModAPI.Audio
{
	/// <summary>Sound playback service supporting one-shot clips and cached, randomized mod sounds.</summary>
	/// <remarks>
	/// Direct port of ModManager.PlaySound / ModdedSound (docs/modapi-plan.md §4.3). Driven every
	/// frame by LokrModAPIPlugin.Update() calling the internal Update() below -- same play/stop
	/// bookkeeping as the original, just no longer living on a MonoBehaviour itself.
	/// </remarks>
	public sealed class ModAudioService
	{
		private readonly Dictionary<ModdedSound, bool> playingSounds = new Dictionary<ModdedSound, bool>();
		private readonly Dictionary<string, ModdedSound> sounds = new Dictionary<string, ModdedSound>();

		/// <summary>Fire-and-forget playback of an already-resolved AudioClip via a transient, self-cleaning AudioSource.</summary>
		/// <remarks>
		/// Unlike PlaySound below, this has no notion of a mod folder and doesn't cache/reuse
		/// sources, since it's meant for one-shot playback of whatever clip the caller already
		/// produced (e.g. from CharacterAPI.ResolveSound).
		/// </remarks>
		public void PlayClip(AudioClip clip)
		{
			if (clip == null)
			{
				return;
			}
			GameObject gameObject = new GameObject("ModSoundOneShot");
			AudioSource audioSource = gameObject.AddComponent<AudioSource>();
			audioSource.clip = clip;
			audioSource.volume = UserSettings.Get("IronhideUserSoundFXVolumeEditorPref", 1f);
			audioSource.Play();
			Object.Destroy(gameObject, clip.length + 0.1f);
		}

		/// <summary>Plays (or restarts) a cached, randomized mod sound for a unit/event, resolving variant files from the mod folder on first use.</summary>
		public void PlaySound(string eventName, string unitId, string modFolder)
		{
			string key = unitId + eventName;
			if (!sounds.TryGetValue(key, out ModdedSound sound))
			{
				sound = new ModdedSound(eventName, unitId, modFolder);
				sounds.Add(key, sound);
			}
			if (!playingSounds.ContainsKey(sound))
			{
				playingSounds.Add(sound, false);
			}
			playingSounds[sound] = false;
		}

		/// <summary>Per-frame play/stop bookkeeping for sounds registered via PlaySound.</summary>
		internal void Update()
		{
			foreach (KeyValuePair<ModdedSound, bool> keyValuePair in new Dictionary<ModdedSound, bool>(playingSounds))
			{
				ModdedSound key = keyValuePair.Key;
				bool value = keyValuePair.Value;
				if (key.IsSourcePlaying() != value)
				{
					playingSounds.Remove(key);
					key.isPlaying = false;
				}
				else if (!key.isPlaying)
				{
					key.StopAll();
					key.Play();
					playingSounds[key] = true;
				}
			}
		}

		/// <summary>One unit/event's cached set of AudioSources, one per matching variant file found in the mod folder.</summary>
		private sealed class ModdedSound
		{
			/// <summary>Tracks whether this sound's cached variant sources are currently playing, checked each frame by ModAudioService.Update() to decide whether to (re)start them.</summary>
			public bool isPlaying;
			private readonly List<AudioSource> sources;

			/// <summary>Scans the mod's Sounds/&lt;unitId&gt; folder for files matching soundType and creates an AudioSource per match.</summary>
			public ModdedSound(string soundType, string unitId, string modPath)
			{
				sources = new List<AudioSource>();
				GameObject gameObject = new GameObject("ModSound");
				Object.DontDestroyOnLoad(gameObject);
				string soundDir = Path.Combine(modPath, "Sounds", unitId);
				if (!Directory.Exists(soundDir))
				{
					return;
				}
				foreach (string text in Directory.EnumerateFiles(soundDir))
				{
					if (text.Contains(soundType))
					{
						AudioSource audioSource = gameObject.AddComponent<AudioSource>();
						sources.Add(audioSource);
						byte[] wavFile = File.ReadAllBytes(Path.Combine(modPath, "Sounds", unitId, text));
						audioSource.clip = OpenWavParser.ByteArrayToAudioClip(wavFile, "", false);
						audioSource.volume = UserSettings.Get("IronhideUserSoundFXVolumeEditorPref", 1f);
					}
				}
			}

			/// <summary>Plays a random one of the cached variant sources.</summary>
			public void Play()
			{
				sources[UnityEngine.Random.Range(0, sources.Count)].volume = UserSettings.Get("IronhideUserSoundFXVolumeEditorPref", 1f);
				sources[UnityEngine.Random.Range(0, sources.Count)].Play();
				isPlaying = true;
			}

			/// <summary>Whether any of this sound's cached variant sources is currently playing.</summary>
			public bool IsSourcePlaying()
			{
				int num = 0;
				foreach (AudioSource audioSource in sources)
				{
					if (audioSource.isPlaying)
					{
						num++;
					}
				}
				return num > 0;
			}

			/// <summary>Stops every cached variant source.</summary>
			public void StopAll()
			{
				foreach (AudioSource audioSource in sources)
				{
					audioSource.Stop();
				}
				isPlaying = false;
			}
		}
	}
}
