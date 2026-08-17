using HarmonyLib;
using LokrModAPI.Input;

namespace LokrModAPI.Patches
{
	/// <summary>Polls mod hotkeys from the game's touch/input manager Update loop.</summary>
	[HarmonyPatch(typeof(InputManagerBehaviour), "Update")]
	internal static class InputManagerBehaviourUpdatePatch
	{
		private static void Postfix()
		{
			GameInputPoll.Tick("InputManagerBehaviour");
		}
	}
}
