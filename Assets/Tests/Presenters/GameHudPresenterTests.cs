using System;
using System.Collections.Generic;
using Lawnmower.Model.Tests;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Presenters.Tests
{
	public class GameHudPresenterTests
	{
		private static readonly Guid SecondLevel = new Guid("22222222222222222222222222222222");

		private sealed class FakeHud : IGameHudView, IGameMenuNavigator
		{
			public readonly List<string> Log = new List<string>();
			public int Level;
			public bool CanUndo, CanRedo;
			public PauseInfo? Paused;
			public int MainMenuLoads;

			public event Action PauseRequested;
			public event Action ResumeRequested;
			public event Action SettingsRequested;
			public event Action MainMenuRequested;

			public void ShowLevel(int number) => Level = number;

			public void ShowHistory(bool canUndo, bool canRedo)
			{
				CanUndo = canUndo;
				CanRedo = canRedo;
			}

			public void ShowPaused(PauseInfo info) => Paused = info;
			public void HidePaused() => Paused = null;

			public void PressPause() => PauseRequested?.Invoke();
			public void PressResume() => ResumeRequested?.Invoke();
			public void PressSettings() => SettingsRequested?.Invoke();
			public void PressMainMenu() => MainMenuRequested?.Invoke();

			public void LoadMainMenu() => MainMenuLoads++;
		}

		private FakeHud _hud;
		private FakeScreen _screen;
		private FakeInput _input;
		private Progression _progression;
		private GamePresenter _game;
		private GameHudPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			ViewLog log = new ViewLog();
			_hud = new FakeHud();
			_screen = new FakeScreen(log);
			_input = new FakeInput();
			InMemoryProgressStore store = new InMemoryProgressStore();
			_progression = new Progression(new[] { SecondLevel, LevelText.DefaultId }, store);
			_game = new GamePresenter(new GameSessionSetup(LevelText.Parse("grass:1 grass:2 grass:2 grass:2", V(0, 0)), 3),
				new FakeBoard(log), new FakeMower(log), _screen, new FakeNavigator(log), _progression, new FakeClock());
			_game.AttachInput(_input);
			_game.Start();
			_game.BeginPlay();
			_presenter = new GameHudPresenter(_game, _hud, _progression, _hud);
			_presenter.Start();
		}

		[TearDown]
		public void TearDown()
		{
			_presenter.Dispose();
			_game.Dispose();
		}

		[Test]
		public void Start_ShowsTheLevelNumber_AndNoHistory()
		{
			Assert.AreEqual(2, _hud.Level);
			Assert.IsFalse(_hud.CanUndo);
			Assert.IsFalse(_hud.CanRedo);
		}

		[Test]
		public void History_FollowsMovesUndosAndRedos()
		{
			_input.Press(R);
			Assert.IsTrue(_hud.CanUndo);
			Assert.IsFalse(_hud.CanRedo);

			_screen.PressUndo();
			Assert.IsFalse(_hud.CanUndo);
			Assert.IsTrue(_hud.CanRedo);

			_screen.PressRedo();
			Assert.IsTrue(_hud.CanUndo);
			Assert.IsFalse(_hud.CanRedo);

			_screen.PressUndo();
			_input.Press(R);
			Assert.IsFalse(_hud.CanRedo, "a new move clears the redo history");
		}

		[Test]
		public void History_IgnoresBumps()
		{
			_input.Press(L);
			Assert.IsFalse(_hud.CanUndo);
		}

		[Test]
		public void Pause_PausesTheGame_AndShowsTheCard_ResumeCloses()
		{
			_hud.PressPause();
			Assert.IsTrue(_game.IsPaused);
			Assert.IsTrue(_hud.Paused.HasValue);
			Assert.AreEqual(2, _hud.Paused.Value.LevelNumber);
			Assert.AreEqual(0, _hud.Paused.Value.Perfects);
			Assert.AreEqual(2, _hud.Paused.Value.LevelCount);

			_input.Press(R);
			Assert.AreEqual(0, _game.Stats.Moves, "no moves while paused");

			_hud.PressResume();
			Assert.IsFalse(_game.IsPaused);
			Assert.IsFalse(_hud.Paused.HasValue);
			_input.Press(R);
			Assert.AreEqual(1, _game.Stats.Moves);
		}

		[Test]
		public void Pause_SubtitleCountsPerfects()
		{
			_progression.MarkCompleted(SecondLevel, 3, true);
			_hud.PressPause();
			Assert.AreEqual(1, _hud.Paused.Value.Perfects);
		}

		[Test]
		public void MainMenu_LoadsTheMainMenu()
		{
			_hud.PressMainMenu();
			Assert.AreEqual(1, _hud.MainMenuLoads);
		}

		[Test]
		public void Settings_IsRaisedForTheScene()
		{
			int raised = 0;
			_presenter.SettingsRequested += () => raised++;
			_hud.PressSettings();
			Assert.AreEqual(1, raised);
		}

		[Test]
		public void Dispose_StopsListening()
		{
			_presenter.Dispose();
			_hud.PressPause();
			Assert.IsFalse(_game.IsPaused);
		}
	}
}
