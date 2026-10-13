using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class LevelSelectPresenterTests
	{
		private sealed class FakeCatalog : ILevelCatalog
		{
			public IReadOnlyList<LevelSummary> Levels = new[]
			{
				new LevelSummary(0, Guid.NewGuid(), false, isCompleted: true, isPerfect: true),
				new LevelSummary(1, Guid.NewGuid(), false, isNext: true),
				new LevelSummary(2, Guid.NewGuid(), true)
			};

			public event Action Changed;

			public IReadOnlyList<LevelSummary> GetLevels() => Levels;
			public void RaiseChanged() => Changed?.Invoke();
			public bool HasListeners => Changed != null;
		}

		private static readonly Guid[] Ids = Enumerable.Range(1, 5).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();

		private FakeLevelSelectView _view;
		private FakeCatalog _catalog;
		private FakeLevelSelectNavigator _navigator;
		private LevelSelectPresenter _presenter;

		[SetUp]
		public void SetUp()
		{
			_view = new FakeLevelSelectView();
			_catalog = new FakeCatalog();
			_navigator = new FakeLevelSelectNavigator();
			_presenter = new LevelSelectPresenter(_view, _catalog, _navigator);
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
		public void Start_ShowsPerfectsOverTotal()
		{
			Assert.AreEqual((1, 3), _view.Perfects);
		}

		[Test]
		public void Start_ShowsThreeWorlds_WithOnlyBackGardenOpen()
		{
			Assert.AreEqual(0, _view.SelectedWorld);
			CollectionAssert.AreEqual(new[] { "BACK GARDEN", "THE PARK", "SEASIDE" }, _view.Worlds.Select(w => w.Name));
			CollectionAssert.AreEqual(new[] { false, true, true }, _view.Worlds.Select(w => w.IsLocked));
		}

		[Test]
		public void TappingAnUnlockedLevel_PlaysIt()
		{
			_view.Tap(1);
			CollectionAssert.AreEqual(new[] { 1 }, _navigator.Played);
		}

		[Test]
		public void TappingALockedLevel_DoesNotPlayIt_AndSaysWhy()
		{
			_view.Tap(2);
			CollectionAssert.IsEmpty(_navigator.Played);
			CollectionAssert.AreEqual(new[] { 2 }, _view.LockedShown);
			Assert.AreEqual("Finish level 2 first!", _view.Messages.Single());
		}

		[Test]
		public void TappingAnUnknownLevel_DoesNothing()
		{
			_view.Tap(7);
			CollectionAssert.IsEmpty(_navigator.Played);
			CollectionAssert.IsEmpty(_view.LockedShown);
		}

		[Test]
		public void TappingALockedWorld_SaysComingSoon()
		{
			_view.TapWorld(0);
			CollectionAssert.IsEmpty(_view.Messages);

			_view.TapWorld(1);
			_view.TapWorld(2);
			CollectionAssert.AreEqual(new[] { "The Park: coming soon!", "Seaside: coming soon!" }, _view.Messages);
			CollectionAssert.IsEmpty(_navigator.Played);
		}

		[Test]
		public void Back_GoesToTheMainMenu()
		{
			_view.PressBack();
			Assert.AreEqual(1, _navigator.MainMenuCount);
		}

		[Test]
		public void CatalogChanges_RefreshTheLevelsAndThePill()
		{
			_catalog.Levels = new[]
			{
				new LevelSummary(0, Guid.NewGuid(), false, isCompleted: true, isPerfect: true),
				new LevelSummary(1, Guid.NewGuid(), false, isCompleted: true, isPerfect: true),
				new LevelSummary(2, Guid.NewGuid(), false, isNext: true)
			};
			_catalog.RaiseChanged();

			Assert.AreEqual(2, _view.ShowCount);
			Assert.AreEqual((2, 3), _view.Perfects);
			_view.Tap(2);
			CollectionAssert.AreEqual(new[] { 2 }, _navigator.Played);
		}

		[Test]
		public void Dispose_StopsListening()
		{
			_presenter.Dispose();
			Assert.IsFalse(_view.HasListeners);
			Assert.IsFalse(_catalog.HasListeners);
		}

		#region Node states

		[Test]
		public void NodeState_FreshProgress_FirstIsNext_RestLocked()
		{
			Progression progression = new Progression(Ids, new InMemoryProgressStore());

			CollectionAssert.AreEqual(
				new[] { LevelNodeState.Next, LevelNodeState.Locked, LevelNodeState.Locked, LevelNodeState.Locked, LevelNodeState.Locked },
				States(progression));
		}

		[Test]
		public void NodeState_PerfectAndCompletedAreDistinct_AndNextFollowsTheLastCompleted()
		{
			Progression progression = new Progression(Ids, new InMemoryProgressStore());
			progression.MarkCompleted(Ids[0], 7, perfect: true);
			progression.MarkCompleted(Ids[1], 12, perfect: false);

			CollectionAssert.AreEqual(
				new[] { LevelNodeState.Perfect, LevelNodeState.Completed, LevelNodeState.Next, LevelNodeState.Locked, LevelNodeState.Locked },
				States(progression));
		}

		[Test]
		public void NodeState_PerfectIsSticky_AfterALaterOverParFinish()
		{
			Progression progression = new Progression(Ids, new InMemoryProgressStore());
			progression.MarkCompleted(Ids[0], 7, perfect: true);
			progression.MarkCompleted(Ids[0], 20, perfect: false);

			Assert.AreEqual(LevelNodeState.Perfect, States(progression)[0]);
		}

		[Test]
		public void NodeState_UnlockAll_OpensEveryLevel_ButOnlyOneIsNext()
		{
			Progression progression = new Progression(Ids, new InMemoryProgressStore());
			progression.UnlockAll();

			CollectionAssert.AreEqual(
				new[] { LevelNodeState.Next, LevelNodeState.Open, LevelNodeState.Open, LevelNodeState.Open, LevelNodeState.Open },
				States(progression));
		}

		[Test]
		public void NodeState_AllCompleted_HasNoNext()
		{
			Progression progression = new Progression(Ids, new InMemoryProgressStore());
			foreach (Guid id in Ids)
			{
				progression.MarkCompleted(id, 5, perfect: false);
			}

			Assert.IsTrue(States(progression).All(s => s == LevelNodeState.Completed));
		}

		[Test]
		public void PerfectsPill_CountsPerfectsFromProgression()
		{
			Progression progression = new Progression(Ids, new InMemoryProgressStore());
			LevelSelectPresenter presenter = new LevelSelectPresenter(_view, new ProgressionLevelCatalog(progression), _navigator);
			_presenter.Dispose();
			presenter.Start();

			Assert.AreEqual((0, 5), _view.Perfects);
			progression.MarkCompleted(Ids[0], 7, perfect: true);
			progression.MarkCompleted(Ids[1], 30, perfect: false);
			Assert.AreEqual((1, 5), _view.Perfects);
			Assert.AreEqual(progression.PerfectCount, _view.Perfects.perfects);

			progression.ResetProgress();
			Assert.AreEqual((0, 5), _view.Perfects);
			Assert.AreEqual(LevelNodeState.Next, _view.Shown[0].NodeState);
			presenter.Dispose();
		}

		private static LevelNodeState[] States(Progression progression) =>
			new ProgressionLevelCatalog(progression).GetLevels().Select(l => l.NodeState).ToArray();

		#endregion
	}
}
