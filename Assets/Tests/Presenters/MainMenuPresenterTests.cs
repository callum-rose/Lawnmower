using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class MainMenuPresenterTests
	{
		private static readonly Guid[] Levels = Enumerable.Range(1, 3).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();

		private sealed class FakeView : IMainMenuView
		{
			public event Action PlayClicked;
			public event Action LevelsClicked;
			public event Action SettingsClicked;
			public event Action HowToPlayClicked;
			public event Action TitleLongPressed;

			public readonly List<int?> PlayLevels = new List<int?>();
			public readonly List<string> Toasts = new List<string>();

			public int? PlayLevel => PlayLevels.Last();
			public bool HasListeners => PlayClicked != null || LevelsClicked != null || SettingsClicked != null ||
			                            HowToPlayClicked != null || TitleLongPressed != null;

			public void ShowPlayLevel(int? levelNumber) => PlayLevels.Add(levelNumber);
			public void ShowToast(string message) => Toasts.Add(message);

			public void Play() => PlayClicked?.Invoke();
			public void Levels() => LevelsClicked?.Invoke();
			public void Settings() => SettingsClicked?.Invoke();
			public void HowToPlay() => HowToPlayClicked?.Invoke();
			public void LongPressTitle() => TitleLongPressed?.Invoke();
		}

		private sealed class FakeNavigator : IMainMenuNavigator
		{
			public readonly List<string> Log = new List<string>();
			public void PlayLevel(Guid level) => Log.Add("play " + Array.IndexOf(MainMenuPresenterTests.Levels, level));
			public void OpenLevelSelect() => Log.Add("levels");
		}

		private FakeView _view;
		private FakeNavigator _navigator;
		private Progression _progression;
		private MainMenuPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_view = new FakeView();
			_navigator = new FakeNavigator();
			_progression = new Progression(Levels, new InMemoryProgressStore());
			_presenter = new MainMenuPresenter(_view, _progression, _navigator);
			_presenter.Start();
		}

		[Test]
		public void Fresh_PlayShowsAndStartsLevelOne()
		{
			Assert.AreEqual(1, _view.PlayLevel);
			_view.Play();
			CollectionAssert.AreEqual(new[] { "play 0" }, _navigator.Log);
		}

		[Test]
		public void Play_StartsTheNextLevelToPlay()
		{
			_progression.MarkCompleted(Levels[0], 9, false);

			Assert.AreEqual(2, _view.PlayLevel, "the label follows progression changes");
			_view.Play();
			CollectionAssert.AreEqual(new[] { "play 1" }, _navigator.Log);
		}

		[Test]
		public void EveryLevelDone_PlayIsUnnumberedAndOpensLevelSelect()
		{
			foreach (Guid level in Levels)
			{
				_progression.MarkCompleted(level, 9, true);
			}

			Assert.IsNull(_view.PlayLevel);
			_view.Play();
			CollectionAssert.AreEqual(new[] { "levels" }, _navigator.Log);
		}

		[Test]
		public void Levels_OpensLevelSelect()
		{
			_view.Levels();
			CollectionAssert.AreEqual(new[] { "levels" }, _navigator.Log);
		}

		[Test]
		public void SettingsAndHowToPlay_AreRaisedForTheScene()
		{
			int settings = 0, howToPlay = 0;
			_presenter.SettingsRequested += () => settings++;
			_presenter.HowToPlayRequested += () => howToPlay++;

			_view.Settings();
			_view.HowToPlay();
			_view.HowToPlay();

			Assert.AreEqual(1, settings);
			Assert.AreEqual(2, howToPlay);
			CollectionAssert.IsEmpty(_navigator.Log);
		}

		[Test]
		public void B6_LongPressingTheTitle_UnlocksEveryLevelAndSaysSo()
		{
			_view.LongPressTitle();

			Assert.IsTrue(_progression.IsAllUnlocked);
			Assert.IsTrue(Levels.All(_progression.IsUnlocked));
			CollectionAssert.AreEqual(new[] { MainMenuPresenter.UnlockAllMessage }, _view.Toasts);
			Assert.AreEqual(1, _view.PlayLevel, "unlocking doesn't skip the first unfinished level");
		}

		[Test]
		public void Dispose_DetachesFromTheViewAndProgression()
		{
			_presenter.Dispose();
			int shown = _view.PlayLevels.Count;

			_progression.MarkCompleted(Levels[0], 9, false);

			Assert.IsFalse(_view.HasListeners);
			Assert.AreEqual(shown, _view.PlayLevels.Count);
		}
	}
}
