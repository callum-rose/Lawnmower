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
using Game.UI.Screens;
using Lawnmower.Model;
using Lawnmower.Presenters;
using Lawnmower.Services;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UI;
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
		private const float Timeout = 20;

		private InMemoryProgressStore _progress;
		private Keyboard _keyboard;
		private MovementScheme _schemeBefore;
		private bool _tutorialSeenBefore;
		private MowerData _mower;
		private readonly List<Object> _created = new List<Object>();
		private readonly List<string> _errors = new List<string>();

		private sealed class SoundRecorder : ISoundEffects
		{
			public readonly List<SoundEffect> Played = new List<SoundEffect>();
			public void Play(SoundEffect effect) => Played.Add(effect);
		}

		/// <summary>
		/// URP render-pass noise when the editor's Game view isn't drawing (unfocused editor); not game code.
		/// </summary>
		private static bool IsRenderNoise(string message, string stackTrace) =>
			message.Contains("Fake or uninitialized surface") || message.Contains("Renderpass")
			|| stackTrace.Contains("ScriptableRenderContext") || stackTrace.Contains("UnityEngine.Rendering.");

		public override void Setup()
		{
			base.Setup();
			_keyboard = InputSystem.AddDevice<Keyboard>();
			_schemeBefore = MovementSchemeSetting.Current;
			MovementSchemeSetting.Current = MovementScheme.Swipe;

			// Level 1 shows the tutorial until it's been seen; most tests play without it.
			_tutorialSeenBefore = PersistantData.LevelModule.TutorialCompleted.Load();
			PersistantData.LevelModule.TutorialCompleted.Save(true);
			TutorialLauncher.CancelRequest();

			_errors.Clear();
			Application.logMessageReceived += OnLog;

			_progress = new InMemoryProgressStore();
			GameServices.InitialiseForTests(_progress);
			_mower = Load<MowerData>("RegularBunny t:MowerData");
		}

		public override void TearDown()
		{
			MovementSchemeSetting.Current = _schemeBefore;
			PersistantData.LevelModule.TutorialCompleted.Save(_tutorialSeenBefore);
			TutorialLauncher.CancelRequest();

			// ignoreFailingMessages belongs to this test's log scope; resetting it here would re-check the render noise.
			Application.logMessageReceived -= OnLog;

			GameServices.SoundEffectsOverrideForTests = null;
			GameServices.Shutdown();

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

			SoundRecorder sounds = new SoundRecorder();
			GameServices.SoundEffectsOverrideForTests = sounds;
			IReadOnlyList<GridVector> solution = MoveNotation.Parse(level.Solution).ToList();
			yield return Play(game, solution);

			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status);
			Assert.AreEqual(solution.Count, sounds.Played.Count(s => s == SoundEffect.Move), "one move sound per move");
			Assert.AreEqual(SoundEffect.Win, sounds.Played.Last());
			Assert.IsTrue(game.Screen.IsCompletedShown);
			Assert.AreEqual(1, _progress.SaveCount);
			Assert.IsTrue(GameServices.Progression.IsCompleted(level.Id));
			Assert.IsTrue(GameServices.Progression.IsUnlocked(GameServices.Progression.Levels[1]), "completing level 1 unlocks level 2");
		}

		/// <summary>The first stone level and the first spring level, replaying their recorded shortest solutions.</summary>
		[UnityTest]
		public IEnumerator CurveLevel_PlaysThroughToCompleted([Values(3, 6)] int index)
		{
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(index).LevelData);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			yield return Play(game, MoveNotation.Parse(level.Solution));

			Assert.AreEqual(LevelStatus.Completed, game.Simulation.Status);
			Assert.IsTrue(game.Screen.IsCompletedShown);
			Assert.AreEqual(level.Par, game.Presenter.Stats.Moves, "the recorded solution is par");
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
			while (Time.realtimeSinceStartup < end && Object.FindObjectsByType<LevelSelect.LevelNodeView>(FindObjectsSortMode.None).Length < levels.LevelCount)
			{
				yield return null;
			}

			LevelSelect.LevelNodeView[] nodes = Object.FindObjectsByType<LevelSelect.LevelNodeView>(FindObjectsSortMode.None);
			Assert.AreEqual(levels.LevelCount, nodes.Length);
			yield return null;
			AssertNoErrors();

			LevelSelect.LevelNodeView first = nodes.Single(n => n.Number == 1);
			Assert.AreEqual(LevelNodeState.Next, first.State);
			first.Button.onClick.Invoke();

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

			ArrowPadView arrows = game.Screen.ArrowPad;
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
			CollectionAssert.AreEqual(new[] { solution[0] }, requested, "the keyboard works in every scheme");
			Assert.AreEqual(1, game.Presenter.Stats.Moves);

			// Each arrow maps onto one board direction; together they cover all four.
			HashSet<GridVector> directions = new HashSet<GridVector>();
			foreach (Vector2 screenDirection in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
			{
				requested.Clear();
				arrows.Press(screenDirection);
				Assert.AreEqual(1, requested.Count);
				directions.Add(requested[0]);
			}

			game.MoveInput.MoveRequested -= requested.Add;
			Assert.AreEqual(4, directions.Count);
			yield return new WaitForSeconds(0.6f);

			MovementSchemeSetting.Current = MovementScheme.Swipe;
			yield return null;
			Assert.IsFalse(arrows.gameObject.activeInHierarchy);
		}

		[UnityTest]
		public IEnumerator FirstLevel_SolvedAtPar_ShowsLawnPerfect_AndUnlocksLevel2()
		{
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);
			GameScreenView screen = game.Screen;
			Assert.AreEqual("LEVEL 1", screen.Hud.LevelText);
			Assert.AreEqual($"0/{level.Par}", screen.Hud.ProgressText);
			Assert.IsFalse(GameServices.Progression.IsUnlocked(GameServices.Progression.Levels[1]));

			IReadOnlyList<GridVector> solution = MoveNotation.Parse(level.Solution);
			yield return Play(game, solution);

			Assert.AreEqual($"{solution.Count}/{level.Par}", screen.Hud.ProgressText);
			Assert.IsTrue(screen.IsCompletedShown, "Level Complete shows");
			Assert.IsTrue(screen.CompleteCard.IsPerfectShown, "at par: the perfect variant with the tick badge");
			Assert.AreEqual("PERFECT!", screen.CompleteCard.Headline);
			Assert.IsTrue(screen.CompleteCard.NextButton.gameObject.activeSelf);
			Assert.IsFalse(screen.Hud.PauseButton.Interactable, "the HUD is covered");
			Assert.IsTrue(GameServices.Progression.IsUnlocked(GameServices.Progression.Levels[1]), "Level 2 unlocks");
			Assert.IsTrue(GameServices.Progression.IsPerfect(level.Id));
		}

		[UnityTest]
		public IEnumerator Failing_ShowsScalped_UndoLastMoveButton_ResumesPlay()
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
			Assert.IsTrue(game.Screen.IsFailedShown, "the SCALPED! sheet shows");

			game.Screen.FailedSheet.UndoButton.Button.onClick.Invoke();
			yield return new WaitForSeconds(0.5f);
			Assert.AreEqual(LevelStatus.Playing, game.Simulation.Status);
			Assert.IsFalse(game.Screen.IsFailedShown, "the sheet closes");
			Assert.IsFalse(game.Screen.FailedSheet.gameObject.activeSelf);

			yield return Play(game, new[] { solution[played] });
			Assert.AreEqual(played + 1, game.Presenter.Stats.Moves, "play resumes");
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator Pause_BlocksMoves_ResumeContinues()
		{
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			IReadOnlyList<GridVector> solution = MoveNotation.Parse(level.Solution);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			game.Screen.Hud.PauseButton.Button.onClick.Invoke();
			yield return null;
			Assert.IsTrue(game.Presenter.IsPaused);
			Assert.IsTrue(game.Screen.IsPausedShown);
			StringAssert.StartsWith("Level 1 · ", game.Screen.PauseCard.Subtitle);

			yield return Play(game, new[] { solution[0] });
			Assert.AreEqual(0, game.Presenter.Stats.Moves, "paused: moves are ignored");

			game.Screen.PauseCard.SettingsButton.Button.onClick.Invoke();
			yield return null;
			Assert.IsTrue(Lawnmower.Screens.SettingsOverlay.IsOpen, "SETTINGS opens the Settings overlay");
			Lawnmower.Screens.SettingsOverlay.Hide();
			yield return new WaitForSeconds(0.5f);
			Assert.IsFalse(Lawnmower.Screens.SettingsOverlay.IsOpen);
			Assert.IsTrue(game.Screen.IsPausedShown, "closing Settings returns to the Pause card");

			game.Screen.PauseCard.ResumeButton.Button.onClick.Invoke();
			yield return new WaitForSeconds(0.3f);
			Assert.IsFalse(game.Presenter.IsPaused);
			Assert.IsFalse(game.Screen.IsPausedShown);

			yield return Play(game, new[] { solution[0] });
			Assert.AreEqual(1, game.Presenter.Stats.Moves);
		}

		[UnityTest]
		public IEnumerator Tutorial_ShowsTheFirstTimeOnLevel1_AndIsRemembered()
		{
			PersistantData.LevelModule.TutorialCompleted.Save(false);
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			IReadOnlyList<GridVector> solution = MoveNotation.Parse(level.Solution);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);
			yield return null;

			TutorialOverlayView tutorial = game.Screen.Tutorial;
			Assert.IsNotNull(game.Tutorial, "the tutorial runs the first time Level 1 is played");
			Assert.AreEqual(TutorialStep.Swipe, tutorial.Step, "the default scheme is Swipe");
			Assert.IsTrue(tutorial.gameObject.activeInHierarchy);

			yield return Play(game, new[] { solution[0] });
			Assert.AreEqual(TutorialStep.MowTheRest, tutorial.Step);

			tutorial.Dismiss();
			yield return new WaitForSeconds(0.3f);
			Assert.IsNull(game.Tutorial);
			Assert.IsTrue(PersistantData.LevelModule.TutorialCompleted.Load(), "seen is saved");

			// Seen: Level 1 no longer shows it, unless requested (How to play / Replay tutorial).
			yield return StartLevel(level, g => game = g);
			yield return null;
			Assert.IsNull(game.Tutorial);

			TutorialLauncher.Request();
			yield return StartLevel(level, g => game = g);
			yield return null;
			Assert.IsNotNull(game.Tutorial);
		}

		[UnityTest]
		public IEnumerator LevelComplete_OverPar_ShowsLawnDoneWithoutTheBadge()
		{
			LevelData level = Copy(Load<LevelDataManager>("t:LevelDataManager").GetLevel(0).LevelData);
			GameManager game = null;
			yield return StartLevel(level, g => game = g);

			LevelCompleteCardView card = game.Screen.CompleteCard;
			card.Show(new LevelCompletedInfo(new AttemptStats(level.Par + 3, 2, 75), level.Par, true,
				new CompletionResult(level.Par + 3, level.Par + 5, level.Par + 3, false, false, null)), 1);
			yield return new WaitForSeconds(0.3f);

			Assert.AreEqual("DONE!", card.Headline);
			Assert.IsFalse(card.IsPerfectShown);
			Assert.AreEqual($"New best: {level.Par + 3} moves!", card.BestLine);
			Assert.AreEqual("1:15", LevelCompleteCardView.FormatTime(75));
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

		/// <summary>A straight drag up the screen, starting on the board (not on canvas UI), in small steps.</summary>
		private IEnumerator SwipeUp(Touchscreen touchscreen)
		{
			Vector2 start = FindPointOffUi();
			float length = Mathf.Min(Screen.height * 0.25f, 400);
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

		private static Vector2 FindPointOffUi()
		{
			EventSystem eventSystem = EventSystem.current;
			List<RaycastResult> hits = new List<RaycastResult>();
			IEnumerable<Vector2> candidates =
				from y in Enumerable.Range(1, 13).Select(i => i / 20f)
				from x in Enumerable.Range(2, 7).Select(i => i / 10f)
				orderby Mathf.Abs(y - 0.5f) + Mathf.Abs(x - 0.5f)
				select new Vector2(Screen.width * x, Screen.height * y);

			foreach (Vector2 point in candidates)
			{
				hits.Clear();
				eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = point }, hits);
				if (!hits.Any(h => h.module is GraphicRaycaster))
				{
					return point;
				}
			}

			Assert.Fail("no point on the board that isn't covered by UI");
			return default;
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
