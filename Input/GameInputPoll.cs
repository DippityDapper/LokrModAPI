using System;
using System.Collections.Generic;

namespace LokrModAPI.Input
{
	/// <summary>Polls registered hotkeys from game MonoBehaviour Update loops (not BepInEx plugin Update).</summary>
	/// <remarks>
	/// On Linux/Proton bare function keys are often stolen by the desktop before Unity sees them.
	/// Patching InputManagerBehaviour (and SRDebugger's shortcut listener) keeps polling on the
	/// same frames the game already reads input. Plugins register chords here instead of relying
	/// on BaseUnityPlugin.Update() alone.
	/// </remarks>
	public static class GameInputPoll
	{
		private sealed class Handler
		{
			internal string Id;
			internal KeyBinding Binding;
			internal Action Action;
		}

		private static readonly List<Handler> Handlers = new List<Handler>();
		private static int lastHandlerFrame = -1;

		/// <summary>Registers a hotkey handler. Re-registering the same id replaces the prior entry.</summary>
		public static void Register(string id, KeyBinding binding, Action action)
		{
			for (int i = 0; i < Handlers.Count; i++)
			{
				if (Handlers[i].Id == id)
				{
					Handlers[i].Binding = binding;
					Handlers[i].Action = action;
					return;
				}
			}

			Handlers.Add(new Handler
			{
				Id = id,
				Binding = binding,
				Action = action,
			});
		}

		/// <summary>Removes a previously registered hotkey handler.</summary>
		public static void Unregister(string id)
		{
			for (int i = Handlers.Count - 1; i >= 0; i--)
			{
				if (Handlers[i].Id == id)
				{
					Handlers.RemoveAt(i);
				}
			}
		}

		internal static void Tick(string source)
		{
			for (int i = 0; i < Handlers.Count; i++)
			{
				Handler handler = Handlers[i];
				if (!handler.Binding.IsDown())
				{
					continue;
				}

				int frame = UnityEngine.Time.frameCount;
				if (lastHandlerFrame == frame)
				{
					return;
				}

				lastHandlerFrame = frame;
				handler.Action();
				return;
			}
		}
	}
}
