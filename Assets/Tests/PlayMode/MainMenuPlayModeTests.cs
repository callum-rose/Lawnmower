#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels;
using Lawnmower.Presenters;
using Lawnmower.Services;
using MainMenu;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	/// <summary>
	/// The MainMenu scene: Play / Levels navigation into Level Select and Level 1, the hidden unlock-all long press,
	/// and the Settings / How to play hooks. Any logged error or exception fails the test.
	/// </summary>
	public class MainMenuPlayModeTests
	{
		private const float Timeout = 20;

		private InMemoryProgressStore _progress;
		private readonly List<string> _errors = new List<string>();

		private static bool IsRenderNoise(string message, string stackTrace) =>
			message.Contains("Fake or uninitialized surface") || message.Contains("Renderpass")
			|| message.StartsWith("Shader error in 'Ocean'")
			|| stackTrace.Contains("ScriptableRenderContext") || stackTrace.Contains("UnityEngine.Rendering.");

		[SetUp]
		public void SetUp()
		{
			_errors.Clear();
			Application.logMessageReceived += OnLog;
			_progress = new InMemoryProgressStore();
			GameServices.InitialiseForTests(_progress);
		}

		[TearDown]
		public void TearDown()
		{
			Application.logMessageReceived -= OnLog;
			GameServices.Shutdown();
		}

		[UnityTest]
		public IEnumerator MainMenu_Levels_OpensLevelSelect_AndLevelOneStartsTheGame()
		{
			MainMenuView menu = null;
			yield return OpenMainMenu(m => menu = m);
			Assert.AreEqual("PLAY · LEVEL 1", menu.PlayLabel);

			menu.LevelsButton.Button.onClick.Invoke();

			// Level Select: the garden path of level nodes; level 1 is the NEXT! node on fresh progress.
			LevelDataManager levels = Load<LevelDataManager>("t:LevelDataManager");
			float end = Time.realtimeSinceStartup + Timeout;
			while (Time.realtimeSinceStartup < end &&
			       Object.FindObjectsByType<LevelSelect.LevelNodeView>(FindObjectsSortMode.None).Length < levels.LevelCount)
			{
				yield return null;
			}

			LevelSelect.LevelNodeView first = Object.FindObjectsByType<LevelSelect.LevelNodeView>(FindObjectsSortMode.None)
				.FirstOrDefault(n => n.Number == 1);
			Assert.IsNotNull(first, "Level Select never showed its levels");
			yield return null;
			first.Button.onClick.Invoke();

			yield return WaitForGame(levels.GetLevel(0).LevelData.Id);
		}

		[UnityTest]
		public IEnumerator MainMenu_Play_StartsTheNextLevelToPlay()
		{
			LevelDataManager levels = Load<LevelDataManager>("t:LevelDataManager");
			GameServices.Progression.MarkCompleted(levels.GetLevel(0).LevelData.Id, 99, false);

			MainMenuView menu = null;
			yield return OpenMainMenu(m => menu = m);
			Assert.AreEqual("PLAY · LEVEL 2", menu.PlayLabel);

			menu.PlayButton.Button.onClick.Invoke();

			yield return WaitForGame(levels.GetLevel(1).LevelData.Id);
		}

		[UnityTest]
		public IEnumerator HoldingTheTitle_UnlocksEveryLevel_WithAToast()
		{
			MainMenuView menu = null;
			yield return OpenMainMenu(m => menu = m);
			Assert.IsFalse(GameServices.Progression.IsAllUnlocked);

			// A short press does nothing.
			PointerDown(menu.Title);
			yield return new WaitForSecondsRealtime(0.5f);
			PointerUp(menu.Title);
			yield return new WaitForSecondsRealtime(menu.Title.HoldSeconds);
			Assert.IsFalse(GameServices.Progression.IsAllUnlocked, "a short press unlocked everything");

			PointerDown(menu.Title);
			yield return new WaitForSecondsRealtime(menu.Title.HoldSeconds + 0.2f);
			PointerUp(menu.Title);

			Assert.IsTrue(GameServices.Progression.IsAllUnlocked);
			Assert.IsTrue(GameServices.Progression.Levels.All(GameServices.Progression.IsUnlocked));
			Assert.IsTrue(menu.Toast.IsShowing, "no confirmation toast");
			Assert.AreEqual(MainMenuPresenter.UnlockAllMessage, menu.ToastMessage);
			Assert.AreEqual(1, _progress.SaveCount, "unlock-all is saved");
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator SettingsAndHowToPlay_RaiseThePresenterHooks()
		{
			MainMenuView menu = null;
			yield return OpenMainMenu(m => menu = m);
			MainMenuPresenter presenter = Object.FindAnyObjectByType<MainMenuSceneManager>().Presenter;
			int settings = 0, howToPlay = 0;
			presenter.SettingsRequested += () => settings++;
			presenter.HowToPlayRequested += () => howToPlay++;

			menu.SettingsButton.Button.onClick.Invoke();
			menu.HowToPlayButton.Button.onClick.Invoke();
			yield return null;

			Assert.AreEqual(1, settings);
			Assert.AreEqual(1, howToPlay);
			Assert.AreEqual("MainMenu", Object.FindAnyObjectByType<MainMenuSceneManager>().gameObject.scene.name, "stays on the menu");
			AssertNoErrors();
		}

		#region Helpers

		private IEnumerator OpenMainMenu(Action<MainMenuView> opened)
		{
			LogAssert.ignoreFailingMessages = true;
			MainMenuView previous = Object.FindAnyObjectByType<MainMenuView>();
			ViewManager.Instance.Load(UnityScene.MainMenu);

			float end = Time.realtimeSinceStartup + Timeout;
			MainMenuView menu = null;
			while (Time.realtimeSinceStartup < end)
			{
				menu = Object.FindAnyObjectByType<MainMenuView>();
				MainMenuSceneManager manager = Object.FindAnyObjectByType<MainMenuSceneManager>();
				if (menu != null && menu != previous && manager != null && manager.Presenter != null)
				{
					break;
				}

				yield return null;
			}

			Assert.IsNotNull(menu, "the MainMenu scene never opened");
			Assert.AreEqual("MainMenu", menu.gameObject.scene.name);
			yield return null;
			AssertNoErrors();
			opened(menu);
		}

		private IEnumerator WaitForGame(Guid level)
		{
			float end = Time.realtimeSinceStartup + Timeout;
			GameManager game = null;
			while (Time.realtimeSinceStartup < end && (game == null || game.Simulation == null))
			{
				game = Object.FindAnyObjectByType<GameManager>();
				yield return null;
			}

			Assert.IsNotNull(game?.Simulation, "the Game scene never started");
			Assert.AreEqual(level, game.Simulation.Level.Id);
			yield return null;
			AssertNoErrors();
		}

		private static void PointerDown(Component target)
		{
			var data = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerDownHandler);
		}

		private static void PointerUp(Component target)
		{
			var data = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(target.gameObject, data, ExecuteEvents.pointerUpHandler);
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
