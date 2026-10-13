#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Lawnmower.Model;
using Lawnmower.Presenters;
using Lawnmower.Services;
using Lawnmower.Solver;
using MainMenu;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	/// <summary>
	/// The whole loop a player goes through, driven only through the UI buttons and a test keyboard:
	/// Main Menu → Level Select → Level 1, solved with the solver's shortest solution; Level Complete; back to Level
	/// Select where Level 2 is now the NEXT! node; Level 2, where a ruining move shows SCALPED!, Undo Last Move
	/// resumes play, and the rest of the solution completes it. Any logged error or exception fails the test.
	/// </summary>
	public class FullPlayThroughTests : InputTestFixture
	{
		private const float Timeout = 20;

		private Keyboard _keyboard;
		private InMemoryProgressStore _progress;
		private MovementScheme _schemeBefore;
		private bool _tutorialSeenBefore;
		private readonly List<string> _errors = new List<string>();

		private static bool IsRenderNoise(string message, string stackTrace) =>
			message.Contains("Fake or uninitialized surface") || message.Contains("Renderpass")
			|| stackTrace.Contains("ScriptableRenderContext") || stackTrace.Contains("UnityEngine.Rendering.");

		public override void Setup()
		{
			base.Setup();
			_keyboard = InputSystem.AddDevice<Keyboard>();
			_schemeBefore = MovementSchemeSetting.Current;
			MovementSchemeSetting.Current = MovementScheme.Swipe;
			_tutorialSeenBefore = PersistantData.LevelModule.TutorialCompleted.Load();
			PersistantData.LevelModule.TutorialCompleted.Save(true);
			TutorialLauncher.CancelRequest();

			_errors.Clear();
			Application.logMessageReceived += OnLog;
			_progress = new InMemoryProgressStore();
			GameServices.InitialiseForTests(_progress);
		}

		public override void TearDown()
		{
			Application.logMessageReceived -= OnLog;
			MovementSchemeSetting.Current = _schemeBefore;
			PersistantData.LevelModule.TutorialCompleted.Save(_tutorialSeenBefore);
			TutorialLauncher.CancelRequest();
			GameServices.Shutdown();
			base.TearDown();
		}

		[UnityTest]
		public IEnumerator MainMenu_ToLevel1Solved_ThenLevel2_FailUndoAndFinish()
		{
			LogAssert.ignoreFailingMessages = true;
			LevelDataManager levels = Load<LevelDataManager>("t:LevelDataManager");
			LevelData level1 = levels.GetLevel(0).LevelData;
			LevelData level2 = levels.GetLevel(1).LevelData;

			// Main Menu → Levels.
			MainMenuView menu = null;
			yield return WaitFor(() => menu = Fresh<MainMenuView>(null), "the Main Menu never opened",
				() => ViewManager.Instance.Load(UnityScene.MainMenu));
			yield return WaitFor(() => Object.FindAnyObjectByType<MainMenuSceneManager>()?.Presenter, "the Main Menu presenter never started");
			Assert.AreEqual("PLAY · LEVEL 1", menu.PlayLabel);
			menu.LevelsButton.Button.onClick.Invoke();

			// Level Select → Level 1 (the NEXT! node on fresh progress).
			LevelSelect.LevelNodeView node1 = null;
			yield return WaitFor(() => node1 = Node(1), "Level Select never showed level 1");
			Assert.AreEqual(LevelNodeState.Next, node1.State);
			Assert.AreEqual(LevelNodeState.Locked, Node(2).State);
			yield return null;
			node1.Button.onClick.Invoke();

			GameManager game = null;
			yield return WaitForLevel(null, level1, g => game = g);

			// The solver's shortest solution, replayed as key presses through the scene's input controller.
			SolveResult solved = LevelSolver.Solve(level1.ToDefinition(), new SolverOptions { ExploreAll = false });
			Assert.AreEqual(SolveStatus.Solved, solved.Status);
			Assert.AreEqual(level1.Par, solved.Solution.Count, "solver par matches the stored par");
			yield return Play(solved.Solution);

			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status);
			yield return WaitFor(() => game.Screen.IsCompletedShown ? game : null, "Level Complete never showed");
			Assert.IsTrue(game.Screen.CompleteCard.IsPerfectShown, "solved at par: LAWN PERFECT!");
			Assert.IsTrue(GameServices.Progression.IsUnlocked(level2.Id), "Level 2 unlocks");

			// Back to Level Select: Level 1 is done, Level 2 is the NEXT! node.
			game.Screen.CompleteCard.LevelsButton.Button.onClick.Invoke();
			LevelSelect.LevelNodeView node2 = null;
			yield return WaitFor(() => (node2 = Node(2)) != null && node2.State == LevelNodeState.Next ? node2 : null,
				"Level Select doesn't show Level 2 as next");
			Assert.AreEqual(LevelNodeState.Perfect, Node(1).State);
			yield return null;
			node2.Button.onClick.Invoke();

			GameManager game2 = null;
			yield return WaitForLevel(game, level2, g => game2 = g);
			game = game2;

			// Level 2: follow the solution until a move would ruin the lawn, take it, then Undo Last Move.
			IReadOnlyList<GridVector> solution = LevelSolver.Solve(level2.ToDefinition(), new SolverOptions { ExploreAll = false }).Solution;
			Assert.IsNotNull(solution, "Level 2 is solvable");
			int played = 0;
			GridVector? ruining;
			while (!(ruining = FindRuiningMove(game.Simulation)).HasValue && played < solution.Count - 1)
			{
				yield return Play(new[] { solution[played++] });
			}

			Assert.IsTrue(ruining.HasValue, "no move along the solution ruins Level 2");
			yield return Play(new[] { ruining.Value });
			Assert.AreEqual(LevelStatus.Failed, game.Simulation.Status);
			Assert.IsTrue(game.Screen.IsFailedShown, "SCALPED! shows");

			game.Screen.FailedSheet.UndoButton.Button.onClick.Invoke();
			yield return new WaitForSeconds(0.5f);
			Assert.AreEqual(LevelStatus.Playing, game.Simulation.Status);
			Assert.IsFalse(game.Screen.IsFailedShown, "the sheet closes");

			yield return Play(solution.Skip(played).ToList());
			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status, "play resumed and Level 2 completes");
			Assert.AreEqual(solution.Count, game.Presenter.Stats.Moves, "the undone move doesn't count");
			Assert.AreEqual(1, game.Presenter.Stats.Undos);
			yield return WaitFor(() => game.Screen.IsCompletedShown ? game : null, "Level Complete never showed for Level 2");
			Assert.IsTrue(GameServices.Progression.IsCompleted(level2.Id));
			AssertNoErrors();
		}

		#region Helpers

		private static LevelSelect.LevelNodeView Node(int number) =>
			Object.FindObjectsByType<LevelSelect.LevelNodeView>(FindObjectsSortMode.None).FirstOrDefault(n => n.Number == number);

		private static T Fresh<T>(T previous) where T : Object
		{
			T found = Object.FindAnyObjectByType<T>();
			return found != null && found != previous ? found : null;
		}

		private IEnumerator WaitFor<T>(Func<T> probe, string failure, Action start = null) where T : class
		{
			start?.Invoke();
			float end = Time.realtimeSinceStartup + Timeout;
			while (Time.realtimeSinceStartup < end && probe() == null)
			{
				yield return null;
			}

			Assert.IsNotNull(probe(), failure);
			AssertNoErrors();
		}

		private IEnumerator WaitForLevel(GameManager previous, LevelData level, Action<GameManager> started)
		{
			float end = Time.realtimeSinceStartup + Timeout;
			GameManager game = null;
			while (Time.realtimeSinceStartup < end)
			{
				game = Object.FindAnyObjectByType<GameManager>();
				if (game != null && game != previous && game.Presenter != null && game.Presenter.IsAcceptingInput)
				{
					break;
				}

				yield return null;
			}

			Assert.IsNotNull(game?.Presenter, "the level never started");
			Assert.IsTrue(game.Presenter.IsAcceptingInput, "the level intro never finished");
			Assert.AreEqual(level.Id, game.Simulation.Level.Id);
			AssertNoErrors();
			started(game);
		}

		private IEnumerator Play(IEnumerable<GridVector> moves)
		{
			foreach (GridVector move in moves)
			{
				ButtonControl key = KeyFor(move);
				Press(key, queueEventOnly: true);
				yield return null;
				Release(key, queueEventOnly: true);
				yield return null;
			}

			yield return new WaitForSeconds(0.6f);
			AssertNoErrors();
		}

		private ButtonControl KeyFor(GridVector move)
		{
			if (move == GridVector.Right) return _keyboard.rightArrowKey;
			if (move == GridVector.Left) return _keyboard.leftArrowKey;
			if (move == GridVector.Up) return _keyboard.upArrowKey;
			if (move == GridVector.Down) return _keyboard.downArrowKey;
			throw new ArgumentOutOfRangeException(nameof(move), move, "not a cardinal unit move");
		}

		private static GridVector? FindRuiningMove(LevelSimulation simulation)
		{
			foreach (GridVector d in new[] { GridVector.Up, GridVector.Down, GridVector.Left, GridVector.Right })
			{
				if (simulation.Preview(d).StatusAfter == LevelStatus.Failed)
				{
					return d;
				}
			}

			return null;
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !IsRenderNoise(message, stackTrace ?? ""))
			{
				_errors.Add($"{type}: {message}\n{stackTrace}");
			}
		}

		private void AssertNoErrors() => CollectionAssert.IsEmpty(_errors, string.Join("\n", _errors));

		private static T Load<T>(string filter) where T : Object
		{
			string[] guids = AssetDatabase.FindAssets(filter);
			Assert.IsNotEmpty(guids, filter);
			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
		}

		#endregion
	}
}
#endif
