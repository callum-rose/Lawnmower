using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class LevelSelectPresenterTests
	{
		private sealed class FakeView : ILevelSelectView
		{
			public event Action<int> LevelChosen;
			public IReadOnlyList<LevelSummary> Shown;

			public void ShowLevels(IReadOnlyList<LevelSummary> levels) => Shown = levels;
			public void Tap(int index) => LevelChosen?.Invoke(index);
			public bool HasListeners => LevelChosen != null;
		}

		private sealed class FakeCatalog : ILevelCatalog
		{
			public IReadOnlyList<LevelSummary> GetLevels() => new[]
			{
				new LevelSummary(0, Guid.NewGuid(), false),
				new LevelSummary(1, Guid.NewGuid(), false),
				new LevelSummary(2, Guid.NewGuid(), true)
			};
		}

		private sealed class FakeNavigator : ILevelSelectNavigator
		{
			public readonly List<int> Played = new List<int>();
			public void PlayLevel(int index) => Played.Add(index);
		}

		private FakeView _view;
		private FakeNavigator _navigator;
		private LevelSelectPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_view = new FakeView();
			_navigator = new FakeNavigator();
			_presenter = new LevelSelectPresenter(_view, new FakeCatalog(), _navigator);
			_presenter.Start();
		}

		[Test]
		public void Start_ShowsEveryLevelNumberedFromOne()
		{
			Assert.AreEqual(3, _view.Shown.Count);
			Assert.AreEqual(1, _view.Shown[0].Number);
			Assert.IsTrue(_view.Shown[2].IsLocked);
		}

		[Test]
		public void TappingAnUnlockedLevel_PlaysIt()
		{
			_view.Tap(1);
			CollectionAssert.AreEqual(new[] { 1 }, _navigator.Played);
		}

		[Test]
		public void TappingALockedOrUnknownLevel_DoesNothing()
		{
			_view.Tap(2);
			_view.Tap(7);
			CollectionAssert.IsEmpty(_navigator.Played);
		}

		[Test]
		public void Dispose_StopsListening()
		{
			_presenter.Dispose();
			Assert.IsFalse(_view.HasListeners);
		}
	}
}
