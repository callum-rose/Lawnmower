#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using LevelSelect;
using Lawnmower.Presenters;
using Lawnmower.Screens;
using Lawnmower.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	/// <summary>The real LevelSelect scene and Settings overlay on in-memory progress and settings.</summary>
	public class LevelSelectSettingsTests
	{
		private const float Timeout = 20;

		private InMemoryProgressStore _progress;
		private InMemorySettingsStore _settings;
		private MovementScheme _schemeBefore;
		private readonly List<string> _errors = new List<string>();

		private static bool IsRenderNoise(string message, string stackTrace) =>
			message.Contains("Fake or uninitialized surface") || message.Contains("Renderpass")
			|| message.StartsWith("Shader error in 'Ocean'")
			|| stackTrace.Contains("ScriptableRenderContext") || stackTrace.Contains("UnityEngine.Rendering.");

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			_errors.Clear();
			Application.logMessageReceived += OnLog;
			_progress = new InMemoryProgressStore();
			_settings = new InMemorySettingsStore();
			GameServices.InitialiseForTests(_progress, _settings);
		}

		[TearDown]
		public void TearDown()
		{
			Application.logMessageReceived -= OnLog;
			SettingsOverlay.ReplayTutorialHook = null;
			if (SettingsOverlay.Current != null)
			{
				Object.Destroy(SettingsOverlay.Current.gameObject);
			}

			GameServices.Shutdown();
			MovementSchemeSetting.Current = _schemeBefore;
		}

		private void OnLog(string message, string stackTrace, LogType type)
		{
			if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !IsRenderNoise(message, stackTrace))
			{
				_errors.Add(message);
			}
		}

		private void AssertNoErrors() => CollectionAssert.IsEmpty(_errors, string.Join("\n", _errors));

		[UnityTest]
		public IEnumerator LevelSelect_NodesShowLockedCompletedPerfectAndNext()
		{
			LogAssert.ignoreFailingMessages = true;
			Progression progression = GameServices.Progression;
			progression.MarkCompleted(progression.Levels[0], 7, perfect: true);
			progression.MarkCompleted(progression.Levels[1], 30, perfect: false);

			LevelSelectScreenView screen = null;
			yield return OpenLevelSelect(s => screen = s);

			LevelNodeState[] states = screen.Nodes.Select(n => n.State).ToArray();
			Assert.AreEqual(progression.LevelCount, states.Length);
			Assert.AreEqual(LevelNodeState.Perfect, states[0]);
			Assert.AreEqual(LevelNodeState.Completed, states[1]);
			Assert.AreEqual(LevelNodeState.Next, states[2]);
			Assert.IsTrue(states.Skip(3).All(s => s == LevelNodeState.Locked));
			Assert.AreEqual($"1/{progression.LevelCount}", screen.PerfectsPill.Label.text);
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator LevelSelect_LockedNodeAndLockedWorld_OnlyShowAToast()
		{
			LogAssert.ignoreFailingMessages = true;
			LevelSelectScreenView screen = null;
			yield return OpenLevelSelect(s => screen = s);

			screen.Nodes[3].Button.onClick.Invoke();
			yield return null;
			Assert.IsTrue(screen.Toast.IsShowing);
			StringAssert.Contains("Finish level 3", screen.Toast.Message.text);

			screen.WorldTabs.Press(1);
			yield return null;
			StringAssert.Contains("coming soon", screen.Toast.Message.text);

			// Still on Level Select: nothing loaded the Game scene.
			for (int i = 0; i < 10; i++)
			{
				yield return null;
			}

			Assert.IsNotNull(Object.FindAnyObjectByType<LevelSelectScreenView>());
			Assert.IsNull(Object.FindAnyObjectByType<Game.Core.GameManager>());
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator LevelSelect_Back_OpensTheMainMenu()
		{
			LogAssert.ignoreFailingMessages = true;
			LevelSelectScreenView screen = null;
			yield return OpenLevelSelect(s => screen = s);

			screen.BackButton.Button.onClick.Invoke();
			float end = Time.realtimeSinceStartup + Timeout;
			while (Time.realtimeSinceStartup < end && Object.FindAnyObjectByType<MainMenu.MainMenuView>() == null)
			{
				yield return null;
			}

			Assert.IsNotNull(Object.FindAnyObjectByType<MainMenu.MainMenuView>(), "Back never reached the Main Menu");
			yield return null;
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator Settings_ResetProgress_AsksForConfirmationFirst()
		{
			LogAssert.ignoreFailingMessages = true;
			Progression progression = GameServices.Progression;
			progression.MarkCompleted(progression.Levels[0], 7, perfect: true);

			SettingsOverlayView overlay = SettingsOverlay.Show();
			Assert.IsNotNull(overlay);
			Assert.IsTrue(SettingsOverlay.IsOpen);
			yield return null;
			SettingsView view = overlay.View;
			Assert.IsFalse(view.ResetModal.gameObject.activeSelf);

			Press(view.ResetProgressRow);
			yield return null;
			Assert.IsTrue(view.ResetModal.IsOpen, "the confirmation opens");
			Assert.AreEqual(1, progression.CompletedCount, "nothing is wiped yet");

			view.CancelReset.Button.onClick.Invoke();
			yield return new WaitForSecondsRealtime(0.3f);
			Assert.IsFalse(view.ResetModal.gameObject.activeSelf);
			Assert.AreEqual(1, progression.CompletedCount, "cancel keeps progress");

			Press(view.ResetProgressRow);
			yield return null;
			view.ConfirmReset.Button.onClick.Invoke();
			yield return null;
			Assert.AreEqual(0, progression.CompletedCount);
			Assert.AreEqual(0, progression.PerfectCount);
			Assert.IsTrue(view.Toast.IsShowing);

			bool closed = false;
			SettingsOverlay.Closed += () => closed = true;
			view.BackButton.Button.onClick.Invoke();
			yield return new WaitForSecondsRealtime(0.4f);
			Assert.IsTrue(closed);
			Assert.IsFalse(SettingsOverlay.IsOpen);
			AssertNoErrors();
		}

		[UnityTest]
		public IEnumerator Settings_ControlsWriteTheSavedSettings()
		{
			LogAssert.ignoreFailingMessages = true;
			SettingsView view = SettingsOverlay.Show().View;
			yield return null;
			GameSettings settings = GameServices.Settings;

			view.Graphics.Select(0);
			view.Movement.Select(1);
			view.Haptics.Toggle();
			view.Music.Value = 0.2f;
			view.Sounds.Value = 0.4f;
			yield return null;

			Assert.AreEqual(GraphicsQuality.Low, settings.Graphics);
			Assert.AreEqual(MovementScheme.Arrows, settings.Movement);
			Assert.IsFalse(settings.Haptics);
			Assert.AreEqual(0.2f, settings.MusicVolume, 1e-4f);
			Assert.AreEqual(0.4f, settings.SoundsVolume, 1e-4f);
			Assert.AreEqual(GraphicsQuality.Low, _settings.Load().graphics, "saved");

			bool replayed = false;
			SettingsOverlay.ReplayTutorialHook = () => replayed = true;
			Press(view.ReplayTutorialRow);
			yield return new WaitForSecondsRealtime(0.4f);
			Assert.IsTrue(replayed, "Replay tutorial closes Settings and calls the hook");
			Assert.IsFalse(SettingsOverlay.IsOpen);
			AssertNoErrors();
		}

		private static void Press(BunnyHeaven.UI.ListRowView row) => row.GetComponent<Button>().onClick.Invoke();

		private static IEnumerator OpenLevelSelect(Action<LevelSelectScreenView> found)
		{
			// A Level Select left open by an earlier test is still around until the new scene replaces it.
			LevelSelectScreenView previous = Object.FindAnyObjectByType<LevelSelectScreenView>();
			ViewManager.Instance.Load(UnityScene.LevelSelect);
			float end = Time.realtimeSinceStartup + Timeout;
			LevelSelectScreenView screen = null;

			while (Time.realtimeSinceStartup < end && (screen == null || screen == previous || screen.Nodes.Count == 0))
			{
				screen = Object.FindAnyObjectByType<LevelSelectScreenView>();
				yield return null;
			}

			Assert.IsNotNull(screen, "LevelSelect never loaded");
			Assert.IsNotEmpty(screen.Nodes);
			yield return null;
			found(screen);
		}
	}
}
#endif
