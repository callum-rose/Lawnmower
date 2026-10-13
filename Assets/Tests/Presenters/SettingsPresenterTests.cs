using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class SettingsPresenterTests
	{
		private sealed class FakeView : ISettingsView
		{
			public GraphicsQuality Graphics;
			public MovementScheme Movement;
			public bool Haptics;
			public float Music = -1;
			public float Sounds = -1;
			public string Version;
			public bool IsConfirmationOpen;
			public readonly List<string> Messages = new List<string>();

			public event Action<GraphicsQuality> GraphicsChosen;
			public event Action<MovementScheme> MovementChosen;
			public event Action<bool> HapticsChosen;
			public event Action<float> MusicVolumeChosen;
			public event Action<float> SoundsVolumeChosen;
			public event Action ReplayTutorialRequested;
			public event Action ResetProgressRequested;
			public event Action ResetConfirmed;
			public event Action ResetCancelled;
			public event Action BackRequested;

			public void ShowGraphics(GraphicsQuality quality) => Graphics = quality;
			public void ShowMovement(MovementScheme scheme) => Movement = scheme;
			public void ShowHaptics(bool isOn) => Haptics = isOn;
			public void ShowMusicVolume(float volume) => Music = volume;
			public void ShowSoundsVolume(float volume) => Sounds = volume;
			public void ShowVersion(string version) => Version = version;
			public void ShowResetConfirmation(bool isOpen) => IsConfirmationOpen = isOpen;
			public void ShowMessage(string message) => Messages.Add(message);

			public void ChooseGraphics(GraphicsQuality q) => GraphicsChosen?.Invoke(q);
			public void ChooseMovement(MovementScheme s) => MovementChosen?.Invoke(s);
			public void ChooseHaptics(bool on) => HapticsChosen?.Invoke(on);
			public void ChooseMusic(float v) => MusicVolumeChosen?.Invoke(v);
			public void ChooseSounds(float v) => SoundsVolumeChosen?.Invoke(v);
			public void PressReplayTutorial() => ReplayTutorialRequested?.Invoke();
			public void PressReset() => ResetProgressRequested?.Invoke();
			public void PressConfirm() => ResetConfirmed?.Invoke();
			public void PressCancel() => ResetCancelled?.Invoke();
			public void PressBack() => BackRequested?.Invoke();

			public bool HasListeners => GraphicsChosen != null || MovementChosen != null || HapticsChosen != null
				|| MusicVolumeChosen != null || SoundsVolumeChosen != null || ReplayTutorialRequested != null
				|| ResetProgressRequested != null || ResetConfirmed != null || ResetCancelled != null || BackRequested != null;
		}

		private sealed class FakeNavigator : ISettingsNavigator
		{
			public int Closed;
			public int Tutorials;
			public void Close() => Closed++;
			public void ReplayTutorial() => Tutorials++;
		}

		private static readonly Guid[] Levels = Enumerable.Range(1, 3).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();

		private MovementScheme _schemeBefore;
		private InMemorySettingsStore _settingsStore;
		private GameSettings _settings;
		private Progression _progression;
		private FakeView _view;
		private FakeNavigator _navigator;
		private SettingsPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			_settingsStore = new InMemorySettingsStore(new SettingsData
			{
				graphics = GraphicsQuality.High,
				movement = MovementScheme.Swipe,
				haptics = true,
				musicVolume = 0.6f,
				soundsVolume = 0.88f
			});
			_settings = new GameSettings(_settingsStore);
			_progression = new Progression(Levels, new InMemoryProgressStore());
			_progression.MarkCompleted(Levels[0], 7, true);
			_progression.MarkCompleted(Levels[1], 9, false);
			_view = new FakeView();
			_navigator = new FakeNavigator();
			_presenter = new SettingsPresenter(_view, _settings, _progression, _navigator, "0.1");
			_presenter.Start();
		}

		[TearDown]
		public void TearDown()
		{
			_presenter.Dispose();
			MovementSchemeSetting.Current = _schemeBefore;
		}

		[Test]
		public void Start_ShowsTheSavedSettings_AndTheVersion()
		{
			Assert.AreEqual(GraphicsQuality.High, _view.Graphics);
			Assert.AreEqual(MovementScheme.Swipe, _view.Movement);
			Assert.IsTrue(_view.Haptics);
			Assert.AreEqual(0.6f, _view.Music, 1e-6f);
			Assert.AreEqual(0.88f, _view.Sounds, 1e-6f);
			Assert.AreEqual("0.1", _view.Version);
			Assert.IsFalse(_view.IsConfirmationOpen);
		}

		[Test]
		public void EveryControl_WritesItsSetting_AndSaves()
		{
			_view.ChooseGraphics(GraphicsQuality.Low);
			_view.ChooseMovement(MovementScheme.Arrows);
			_view.ChooseHaptics(false);
			_view.ChooseMusic(0.25f);
			_view.ChooseSounds(0f);

			Assert.AreEqual(GraphicsQuality.Low, _settings.Graphics);
			Assert.AreEqual(MovementScheme.Arrows, _settings.Movement);
			Assert.AreEqual(MovementScheme.Arrows, MovementSchemeSetting.Current);
			Assert.IsFalse(_settings.Haptics);
			Assert.AreEqual(0.25f, _settings.MusicVolume, 1e-6f);
			Assert.AreEqual(0f, _settings.SoundsVolume, 1e-6f);

			SettingsData saved = _settingsStore.Load();
			Assert.AreEqual(GraphicsQuality.Low, saved.graphics);
			Assert.AreEqual(MovementScheme.Arrows, saved.movement);
			Assert.IsFalse(saved.haptics);
			Assert.AreEqual(5, _settingsStore.SaveCount);
		}

		[Test]
		public void TheView_FollowsSettingsChangedElsewhere()
		{
			_settings.Graphics = GraphicsQuality.Low;
			_settings.Haptics = false;
			_settings.SoundsVolume = 0.1f;

			Assert.AreEqual(GraphicsQuality.Low, _view.Graphics);
			Assert.IsFalse(_view.Haptics);
			Assert.AreEqual(0.1f, _view.Sounds, 1e-6f);
		}

		[Test]
		public void ResetProgress_OnlyOpensTheConfirmation()
		{
			_view.PressReset();

			Assert.IsTrue(_view.IsConfirmationOpen);
			Assert.IsTrue(_presenter.IsConfirmingReset);
			Assert.AreEqual(2, _progression.CompletedCount, "nothing is wiped before the player confirms");
			CollectionAssert.IsEmpty(_view.Messages);
		}

		[Test]
		public void ResetProgress_Confirmed_WipesProgress()
		{
			_view.PressReset();
			_view.PressConfirm();

			Assert.IsFalse(_view.IsConfirmationOpen);
			Assert.AreEqual(0, _progression.CompletedCount);
			Assert.AreEqual(0, _progression.PerfectCount);
			Assert.IsFalse(_progression.IsUnlocked(Levels[1]));
			CollectionAssert.AreEqual(new[] { SettingsPresenter.ProgressResetMessage }, _view.Messages);
		}

		[Test]
		public void ResetProgress_Cancelled_KeepsProgress()
		{
			_view.PressReset();
			_view.PressCancel();

			Assert.IsFalse(_view.IsConfirmationOpen);
			Assert.AreEqual(2, _progression.CompletedCount);
			Assert.AreEqual(1, _progression.PerfectCount);
		}

		[Test]
		public void Confirm_WithoutTheModalOpen_DoesNothing()
		{
			_view.PressConfirm();
			Assert.AreEqual(2, _progression.CompletedCount);

			_view.PressReset();
			_view.PressCancel();
			_view.PressConfirm();
			Assert.AreEqual(2, _progression.CompletedCount, "a stale confirm after cancelling doesn't wipe");
		}

		[Test]
		public void Back_WhileConfirming_ClosesOnlyTheConfirmation()
		{
			_view.PressReset();
			_view.PressBack();

			Assert.IsFalse(_view.IsConfirmationOpen);
			Assert.AreEqual(0, _navigator.Closed);
			Assert.AreEqual(2, _progression.CompletedCount);

			_view.PressBack();
			Assert.AreEqual(1, _navigator.Closed);
		}

		[Test]
		public void ReplayTutorial_GoesToTheTutorial()
		{
			_view.PressReplayTutorial();
			Assert.AreEqual(1, _navigator.Tutorials);
		}

		[Test]
		public void Dispose_StopsListeningToTheViewAndTheSettings()
		{
			_presenter.Dispose();
			Assert.IsFalse(_view.HasListeners);

			_settings.Graphics = GraphicsQuality.Low;
			Assert.AreEqual(GraphicsQuality.High, _view.Graphics);
		}
	}
}
