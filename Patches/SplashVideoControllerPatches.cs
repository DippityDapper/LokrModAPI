using HarmonyLib;
using Ironhide.Legends;
using Ironhide.Legends.View.Screens.Transition;

namespace LokrModAPI.Patches
{
	/// <summary>Skips the intro splash video and jumps straight to the main menu when configured to.</summary>
	/// <remarks>The one game-touching patch LokrModAPI owns directly (docs/modapi-plan.md §2) -- reads ModAPI.Config.SkipSplashScreen, which lives here anyway.</remarks>
	[HarmonyPatch(typeof(SplashVideoController), "Awake")]
	internal static class SplashVideoController_Awake_Patch
	{
		/// <summary>Returns false (skipping the original method) and transitions straight to the main screen if SkipSplashScreen is enabled.</summary>
		[HarmonyPrefix]
		private static bool Prefix()
		{
			if (ModAPI.Config.SkipSplashScreen.Value)
			{
				TransitionSceneComponent.TransitionToNextScene("scenes", SceneDB.GetScene("mainScreen"));
				return false;
			}
			return true;
		}
	}
}
