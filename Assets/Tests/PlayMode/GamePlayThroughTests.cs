#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Game.Input;
using Lawnmower.Model;
using Lawnmower.Presenters;
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
	/// Plays levels in the real Game scene: moves are key presses on a test keyboard (New Input System), which go
	/// through the scene's <see cref="MoveInputController"/> into the presenter. Swipes use a test touchscreen.
	/// Any logged error or exception fails the test.
	/// </summary>
	public class GamePlayThroughTests : InputTestFixture
	{
		private sealed class NoProgress : ILevelProgress
		{
			public int Completed;
			public void RecordCompleted(Guid levelId, AttemptStats stats, int par) => Completed++;
		}

		private const float Timeout = 20;

		private NoProgress _progress;
		private Keyboard _keyboard;
		private MovementScheme _schemeBefore;
		private MowerData _mower;
		private readonly List<Object> _created = new List<Object>();
		private readonly List<string> _errors = new List<string>();

		/// <summary>URP render-pass noise when the editor's Game view isn't drawing (unfocused editor); not game code.</summary>
		private static bool IsRenderNoise(string message, string stackTrace) =>
			message.Contains("Fake or uninitialized surface") || message.Contains("Renderpass")
			|| stackTrace.Contains("ScriptableRenderContext") || stackTrace.Contains("UnityEngine.Rendering.");

		public override void Setup()
		{
			base.Setup();
			_keyboard = InputSystem.AddDevice<Keyboard>();
			_schemeBefore = MovementSchemeSetting.Current;
			MovementSchemeSetting.Current = MovementScheme.Swipe;

			_errors.Clear();
			Application.logMessageReceived += OnLog;

			_progress = new NoProgress();
			GameManager.ProgressOverrideForTests = _progress;
			_mower = Load<MowerData>("RegularBunny t:MowerData");
		}

		public override void TearDown()
		{
			MovementSchemeSetting.Current = _schemeBefore;

			// ignoreFailingMessages belongs to this test's log scope; resetting it here would re-check the render noise.
			Application.logMessageReceived -= OnLog;

			GameManager.ProgressOverrideForTests = null;

			foreach (Object o in _created)
			{
				Object.Destroy(o);
			}

			_created.Clear();
			base.TearDown();
		}

		[UnityTest]
		public IEnumerator FirstLevel_PlaysThroughToCompleted()
		{
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			yield return Play(game, MoveNotation.Parse(level.Solution));

			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status);
			Assert.IsTrue(game.Screen.IsCompletedShown);
			Assert.AreEqual(1, _progress.Completed);
		}

		[UnityTest]
		public IEnumerator SpringLevel_PlaysThroughToCompleted()
		{
			string[] guids = AssetDatabase.FindAssets("\"Level Spring Test\" t:LevelData");
			if (guids.Length == 0)
			{
				Assert.Ignore("No spring test level in the project");
			}

			LevelData level = Copy(AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guids[0])));
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			yield return Play(game, MoveNotation.Parse(level.Solution));

			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status);
		}

		[UnityTest]
		public IEnumerator Failing_ThenUndoLastMove_ResumesPlay()
		{
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			IReadOnlyList<GridVector> solution = MoveNotation.Parse(level.Solution);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			int played = 0;
			GridVector? ruining = null;
			while (played < solution.Count - 1 && !(ruining = FindRuiningMove(game.Simulation)).HasValue)
			{
				yield return Play(game, new[] { solution[played++] });
			}

			Assert.IsTrue(ruining.HasValue, "no move along the solution ruins the level");

			yield return Play(game, new[] { ruining.Value });
			Assert.AreEqual(LevelStatus.Failed, game.Simulation.Status);
			Assert.IsTrue(game.Screen.IsFailedShown);

			yield return Play(game, new[] { solution[played] });
			Assert.AreEqual(LevelStatus.Failed, game.Simulation.Status, "the Failed card blocks moves");

			game.Screen.PressUndoLastMove();
			yield return null;
			AssertNoErrors();
			Assert.AreEqual(LevelStatus.Playing, game.Simulation.Status);
			Assert.IsFalse(game.Screen.IsFailedShown);

			yield return Play(game, solution.Skip(played).ToList());
			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status);
			Assert.AreEqual(1, game.Presenter.Stats.Undos);
			Assert.AreEqual(solution.Count, game.Presenter.Stats.Moves, "the undone move doesn't count");
		}

		[UnityTest]
		public IEnumerator LevelSelect_ShowsEveryLevel_AndTheFirstOpensTheGame()
		{
			LogAssert.ignoreFailingMessages = true;
			LevelDataManager levels = Load<LevelDataManager>("t:LevelDataManager");
			GameManager previous = Object.FindAnyObjectByType<GameManager>();

			ViewManager.Instance.Load(UnityScene.LevelSelect);
			float end = Time.realtimeSinceStartup + Timeout;
			UI.Buttons.Button play = null;
			while (Time.realtimeSinceStartup < end && play == null)
			{
				play = Object.FindObjectsByType<UI.Buttons.Button>(FindObjectsSortMode.None).FirstOrDefault(b => b.gameObject.scene.name == "LevelSelect");
				yield return null;
			}

			Assert.IsNotNull(play, "LevelSelect's Play button never appeared");
			yield return null;
			play.OnPointerClick(null);

			while (Time.realtimeSinceStartup < end && Object.FindObjectsByType<LevelSelect.LevelTile>(FindObjectsSortMode.None).Length < levels.LevelCount)
			{
				yield return null;
			}

			LevelSelect.LevelTile[] tiles = Object.FindObjectsByType<LevelSelect.LevelTile>(FindObjectsSortMode.None);
			Assert.AreEqual(levels.LevelCount, tiles.Length);
			AssertNoErrors();

			// Tiles are added to their container in level order.
			LevelSelect.LevelTile first = tiles.OrderBy(t => t.transform.GetSiblingIndex()).First();
			first.OnPointerClick(null);

			GameManager game = null;
			end = Time.realtimeSinceStartup + Timeout;
			while (Time.realtimeSinceStartup < end && (game == null || game == previous || game.Simulation == null))
			{
				game = Object.FindAnyObjectByType<GameManager>();
				yield return null;
			}

			Assert.IsNotNull(game?.Simulation, "the Game scene never started");
			Assert.AreEqual(levels.GetLevel(0).LevelData.Id, game.Simulation.Level.Id);
			yield return null;
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator Swipe_OnTheBoard_RequestsOneMove()
		{
			Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			List<GridVector> requested = new List<GridVector>();
			game.MoveInput.MoveRequested += requested.Add;
			yield return SwipeUp(touchscreen);
			game.MoveInput.MoveRequested -= requested.Add;

			Assert.AreEqual(1, requested.Count, "one swipe, one move");
			Assert.IsTrue(requested[0].IsCardinalUnit);
			yield return new WaitForSeconds(0.6f);
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator ArrowsScheme_IgnoresSwipes_ShowsTheArrows_AndKeepsTheKeyboard()
		{
			Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			global::Game.UI.UiArrowsGraphicInputHandler arrows = Object.FindAnyObjectByType<global::Game.UI.UiArrowsGraphicInputHandler>(FindObjectsInactive.Include);
			Assert.IsNotNull(arrows, "the Game scene has on-screen arrows");
			Assert.IsFalse(arrows.gameObject.activeInHierarchy, "arrows are hidden in the Swipe scheme");

			MovementSchemeSetting.Current = MovementScheme.Arrows;
			yield return null;
			Assert.AreEqual(MovementScheme.Arrows, game.MoveInput.Scheme);
			Assert.IsTrue(arrows.gameObject.activeInHierarchy, "arrows are shown in the Arrows scheme");

			List<GridVector> requested = new List<GridVector>();
			game.MoveInput.MoveRequested += requested.Add;
			yield return SwipeUp(touchscreen);
			Assert.IsEmpty(requested, "swipes are ignored in the Arrows scheme");

			IReadOnlyList<GridVector> solution = MoveNotation.Parse(level.Solution);
			yield return Play(game, new[] { solution[0] });
			game.MoveInput.MoveRequested -= requested.Add;
			CollectionAssert.AreEqual(new[] { solution[0] }, requested, "the keyboard works in every scheme");
			Assert.AreEqual(1, game.Presenter.Stats.Moves);

			MovementSchemeSetting.Current = MovementScheme.Swipe;
			yield return null;
			Assert.IsFalse(arrows.gameObject.activeInHierarchy);
		}

		#region Helpers

		private void OnLog(string message, string stackTrace, LogType type)
		{
			if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
			{
				if (!IsRenderNoise(message, stackTrace ?? ""))
				{
					_errors.Add($"{type}: {message}\n{stackTrace}");
				}
			}
		}

		private void AssertNoErrors()
		{
			CollectionAssert.IsEmpty(_errors, string.Join("\n", _errors));
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

		private IEnumerator StartLevel(LevelData level, Action<GameManager> started)
		{
			// Errors are checked by OnLog instead, so the editor's render noise can be filtered out.
			LogAssert.ignoreFailingMessages = true;

			// A previous test's Game scene may still be loaded until the new one replaces it.
			GameManager previous = Object.FindAnyObjectByType<GameManager>();
			ViewManager.Instance.Load(UnityScene.Game, new GameSetupPassThroughData(_mower, level));

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

		private IEnumerator Play(GameManager game, IEnumerable<GridVector> moves)
		{
			foreach (GridVector move in moves)
			{
				ButtonControl key = KeyFor(move);
				Press(key, queueEventOnly: true);
				yield return null;
				Release(key, queueEventOnly: true);
				yield return null;
			}

			// Let the mower animations finish so the views are exercised too.
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

		/// <summary>A straight drag up the screen from its centre, in small steps.</summary>
		private IEnumerator SwipeUp(Touchscreen touchscreen)
		{
			Vector2 start = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
			float length = Mathf.Min(Screen.height * 0.3f, 400);
			BeginTouch(1, start, queueEventOnly: true, screen: touchscreen);
			yield return null;

			for (int i = 1; i <= 8; i++)
			{
				MoveTouch(1, start + new Vector2(0, length * i / 8), queueEventOnly: true, screen: touchscreen);
				yield return null;
			}

			EndTouch(1, start + new Vector2(0, length), queueEventOnly: true, screen: touchscreen);
			yield return null;
			yield return null;
		}

		/// <summary>A runtime copy, so the scene change can't unload it and nothing writes to the asset.</summary>
		private LevelData Copy(LevelData asset)
		{
			LevelData copy = Object.Instantiate(asset);
			copy.hideFlags = HideFlags.DontUnloadUnusedAsset;
			_created.Add(copy);
			return copy;
		}

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
