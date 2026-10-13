using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Lawnmower.Presenters;
using Lawnmower.Screens;
using Lawnmower.Services;
using UnityEngine;

namespace Game.Tutorial
{
	/// <summary>
	/// Public entry point for "How to play" (Main Menu) and "Replay tutorial" (Settings): requests the tutorial
	/// (<see cref="TutorialLauncher.Request"/>) and loads Level 1, where the Game scene shows it.
	/// Also installs itself as <see cref="SettingsOverlay.ReplayTutorialHook"/>.
	/// </summary>
	public static class TutorialEntry
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Install()
		{
			SettingsOverlay.ReplayTutorialHook ??= PlayTutorial;
		}

		/// <summary>Shows the tutorial on Level 1 with the current mower.</summary>
		public static void PlayTutorial()
		{
			LevelDataManager levels = Resources.Load<GameServicesConfig>(GameServicesConfig.ResourcesPath)?.Levels;
			MowerData mower = CurrentMower();

			if (levels == null || levels.LevelCount == 0 || mower == null)
			{
				Debug.LogWarning("[Tutorial] Can't start the tutorial: no level list or mower loaded.");
				return;
			}

			TutorialLauncher.Request();
			ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(mower, levels.GetLevel(0).LevelData));
		}

		/// <summary>The Game scene's mower, else the current one (Main Menu and Level Select load CurrentMowerManager).</summary>
		private static MowerData CurrentMower()
		{
			GameManager game = Object.FindAnyObjectByType<GameManager>();
			if (game != null && game.Mower != null)
			{
				return game.Mower;
			}

			return Resources.FindObjectsOfTypeAll<CurrentMowerManager>().FirstOrDefault()?.GetCurrent();
		}
	}
}
