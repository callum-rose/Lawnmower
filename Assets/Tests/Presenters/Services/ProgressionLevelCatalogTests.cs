using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class ProgressionLevelCatalogTests
	{
		private static readonly Guid[] Levels = Enumerable.Range(1, 3).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();

		[Test]
		public void Summaries_ComeFromProgression()
		{
			Progression progression = new Progression(Levels, new InMemoryProgressStore());
			progression.MarkCompleted(Levels[0], 6, true);

			IReadOnlyList<LevelSummary> levels = new ProgressionLevelCatalog(progression).GetLevels();

			Assert.AreEqual(3, levels.Count);

			Assert.AreEqual(1, levels[0].Number);
			Assert.IsFalse(levels[0].IsLocked);
			Assert.IsTrue(levels[0].IsCompleted);
			Assert.IsTrue(levels[0].IsPerfect);
			Assert.AreEqual(6, levels[0].Best);
			Assert.IsFalse(levels[0].IsNext);

			Assert.AreEqual(Levels[1], levels[1].Id);
			Assert.IsFalse(levels[1].IsLocked);
			Assert.IsFalse(levels[1].IsCompleted);
			Assert.IsNull(levels[1].Best);
			Assert.IsTrue(levels[1].IsNext);

			Assert.IsTrue(levels[2].IsLocked);
			Assert.IsFalse(levels[2].IsNext);
		}

		[Test]
		public void LockedLevels_CannotBePlayed_UntilTheOneBeforeIsCompleted()
		{
			Progression progression = new Progression(Levels, new InMemoryProgressStore());
			FakeLevelSelectView view = new FakeLevelSelectView();
			FakeLevelSelectNavigator navigator = new FakeLevelSelectNavigator();

			LevelSelectPresenter presenter = new LevelSelectPresenter(view, new ProgressionLevelCatalog(progression), navigator);
			presenter.Start();
			view.Tap(1);
			presenter.Dispose();

			progression.MarkCompleted(Levels[0], 6, false);
			presenter = new LevelSelectPresenter(view, new ProgressionLevelCatalog(progression), navigator);
			presenter.Start();
			view.Tap(1);
			presenter.Dispose();

			CollectionAssert.AreEqual(new[] { 1 }, navigator.Played);
		}
	}
}
