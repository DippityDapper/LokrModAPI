using HarmonyLib;
using LokrModAPI.Input;
using SRDebugger.Services.Implementation;

namespace LokrModAPI.Patches
{
	/// <summary>Fallback poll tick — SRDebugger's shortcut listener Update always runs once loaded.</summary>
	[HarmonyPatch(typeof(KeyboardShortcutListenerService), "Update")]
	internal static class KeyboardShortcutListenerUpdatePatch
	{
		private static void Postfix()
		{
			GameInputPoll.Tick("KeyboardShortcutListener");
		}
	}
}
