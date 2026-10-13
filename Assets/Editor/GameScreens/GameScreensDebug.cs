using System.IO;
using System.Linq;
using Lawnmower.Model;
using Lawnmower.Presenters;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using UnityEditor;
using UnityEngine;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>Play-mode helpers for checking the Game screens by eye: load a level, capture the Game view.</summary>
	public static class GameScreensDebug
	{
		/// <summary>Loads level <paramref name="index"/> (0-based) into the Game scene. Play mode only.</summary>
		public static string LoadLevel(int index)
		{
			if (!Application.isPlaying)
			{
				return "not playing";
			}

			LevelDataManager levels = Find<LevelDataManager>("t:LevelDataManager");
			MowerData mower = Find<MowerData>("EggBunny t:MowerData");
			ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(mower, levels.GetLevel(index).LevelData));
			return $"loading {levels.GetLevel(index).LevelData.name}";
		}

		/// <summary>Writes the Game view (including overlay UI) to <paramref name="path"/> at the end of the frame.</summary>
		public static string Capture(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
			ScreenCapture.CaptureScreenshot(path);
			return path;
		}

		public static string Pause()
		{
			Game.Presenter.Pause();
			return "paused";
		}

		public static string Resume()
		{
			Game.Presenter.Resume();
			return "resumed";
		}

		/// <summary>Plays the level's recorded solution, all at once.</summary>
		public static string PlaySolution(int skipLast = 0)
		{
			GameManager game = Game;
			LevelData level = Find<LevelDataManager>("t:LevelDataManager").Levels
				.First(l => l.Id == game.Simulation.Level.Id);
			var moves = MoveNotation.Parse(level.Solution);
			for (int i = 0; i < moves.Count - skipLast; i++)
			{
				game.Presenter.RequestMove(moves[i]);
			}

			return game.Simulation.Status.ToString();
		}

		/// <summary>Makes the first move that ruins the level, playing the solution until one exists.</summary>
		public static string ForceFail()
		{
			GameManager game = Game;
			LevelData level = Find<LevelDataManager>("t:LevelDataManager").Levels
				.First(l => l.Id == game.Simulation.Level.Id);
			var moves = MoveNotation.Parse(level.Solution);
			GridVector[] directions = { GridVector.Up, GridVector.Down, GridVector.Left, GridVector.Right };
			for (int i = 0; i <= moves.Count; i++)
			{
				foreach (GridVector d in directions)
				{
					if (game.Simulation.Preview(d).StatusAfter == LevelStatus.Failed)
					{
						game.Presenter.RequestMove(d);
						return game.Simulation.Status.ToString();
					}
				}

				game.Presenter.RequestMove(moves[i]);
			}

			return "no ruining move";
		}

		/// <summary>Shows the over-par ("LAWN DONE!") Level Complete card with made-up numbers.</summary>
		public static string ShowDoneVariant()
		{
			GameManager game = Game;
			int par = game.Presenter.Par;
			game.Screen.CompleteCard.Show(new LevelCompletedInfo(new AttemptStats(par + 4, 3, 102), par, true,
				new CompletionResult(par + 4, par + 6, par + 4, false, false, null)), 1);
			return "done variant";
		}

		public static string SetScheme(bool arrows)
		{
			MovementSchemeSetting.Current = arrows ? MovementScheme.Arrows : MovementScheme.Swipe;
			return MovementSchemeSetting.Current.ToString();
		}

		internal static GameManager Game => Object.FindAnyObjectByType<GameManager>();

		private static T Find<T>(string filter) where T : Object
		{
			string[] guids = AssetDatabase.FindAssets(filter);
			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
		}
	}
}
