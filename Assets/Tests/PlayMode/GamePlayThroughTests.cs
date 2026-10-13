#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Game.Mowers;
using Game.Mowers.Input;
using Lawnmower.Model;
using Lawnmower.Presenters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	/// <summary>
	/// Plays levels in the real Game scene: input goes through the mower input channel into the
	/// presenter, exactly as the keyboard/swipe/arrow controls send it. Any logged error or exception fails the test.
	/// </summary>
	public class GamePlayThroughTests
	{
		private sealed class NoProgress : ILevelProgress
		{
			public int Completed;
			public void RecordCompleted(Guid levelId, AttemptStats stats, int par) => Completed++;
		}

		private const float Timeout = 20;

		private NoProgress _progress;
		private MowerInputEventChannel _input;
		private MowerData _mower;
		private readonly List<Object> _created = new List<Object>();
		private readonly List<string> _errors = new List<string>();

		/// <summary>
		/// URP render-pass noise when the editor's Game view isn't drawing (unfocused editor), and the known
		/// Ocean.shader compile error (STATUS.md), logged once by whichever test first draws the ocean; not game code.
		/// </summary>
		private static bool IsRenderNoise(string message, string stackTrace) =>
			message.Contains("Fake or uninitialized surface") || message.Contains("Renderpass")
			|| message.StartsWith("Shader error in 'Ocean'")
			|| stackTrace.Contains("ScriptableRenderContext") || stackTrace.Contains("UnityEngine.Rendering.");

		[SetUp]
		public void SetUp()
		{
			_errors.Clear();
			Application.logMessageReceived += OnLog;

			_progress = new NoProgress();
			GameManager.ProgressOverrideForTests = _progress;
			_input = Load<MowerInputEventChannel>("t:MowerInputEventChannel");
			_mower = Load<MowerData>("RegularBunny t:MowerData");
		}

		[TearDown]
		public void TearDown()
		{
			// ignoreFailingMessages belongs to this test's log scope; resetting it here would re-check the render noise.
			Application.logMessageReceived -= OnLog;

			GameManager.ProgressOverrideForTests = null;

			foreach (Object o in _created)
			{
				Object.Destroy(o);
			}

			_created.Clear();
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
				_input.Raise(move);
				yield return null;
			}

			// Let the mower animations finish so the views are exercised too.
			yield return new WaitForSeconds(0.6f);
			AssertNoErrors();
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
