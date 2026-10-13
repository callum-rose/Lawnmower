using System;
using System.Linq;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class ProgressionTests
	{
		private static readonly Guid[] Levels = Enumerable.Range(1, 4).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();
		private static readonly Guid Unlisted = new Guid("99999999999999999999999999999999");

		private InMemoryProgressStore _store;
		private Progression _progression;

		[SetUp]
		public void SetUp()
		{
			_store = new InMemoryProgressStore();
			_progression = new Progression(Levels, _store);
		}

		private Progression Reload() => new Progression(Levels, _store);

		#region Unlocking

		[Test]
		public void Fresh_OnlyTheFirstLevelIsUnlocked()
		{
			Assert.IsTrue(_progression.IsUnlocked(Levels[0]));
			Assert.IsFalse(_progression.IsUnlocked(Levels[1]));
			Assert.IsFalse(_progression.IsUnlocked(Levels[3]));
			Assert.AreEqual(0, _progression.CompletedCount);
			Assert.AreEqual(0, _progression.PerfectCount);
		}

		[Test]
		public void B5_CompletingALevel_UnlocksTheNextOne()
		{
			_progression.MarkCompleted(Levels[0], 9, false);

			Assert.IsTrue(_progression.IsUnlocked(Levels[1]), "level 2 opens as soon as level 1 is done (B5 was off by one)");
			Assert.IsFalse(_progression.IsUnlocked(Levels[2]));
		}

		[Test]
		public void ReplayingAnEarlierLevel_DoesNotRelockLaterOnes()
		{
			_progression.MarkCompleted(Levels[0], 9, false);
			_progression.MarkCompleted(Levels[1], 9, false);
			_progression.MarkCompleted(Levels[0], 8, false);

			Assert.IsTrue(_progression.IsUnlocked(Levels[2]));
		}

		[Test]
		public void MarkCompleted_ReportsTheLevelItUnlocked()
		{
			Assert.AreEqual(Levels[1], _progression.MarkCompleted(Levels[0], 5, false).UnlockedLevel);
			Assert.IsNull(_progression.MarkCompleted(Levels[0], 5, false).UnlockedLevel, "already open");
			Assert.IsNull(_progression.MarkCompleted(Levels[3], 5, false).UnlockedLevel, "last level");
		}

		[Test]
		public void B6_UnlockAll_OpensEveryLevel_AndIsSaved()
		{
			_progression.UnlockAll();

			Assert.IsTrue(Levels.All(_progression.IsUnlocked));
			Assert.IsTrue(_progression.IsAllUnlocked);
			Assert.IsTrue(Reload().IsUnlocked(Levels[3]));
			Assert.AreEqual(0, _progression.CompletedCount, "unlocking completes nothing");
		}

		[Test]
		public void UnlistedLevels_AreNeverUnlocked_ButCanBeRecorded()
		{
			CompletionResult result = _progression.MarkCompleted(Unlisted, 3, true);

			Assert.IsFalse(_progression.IsUnlocked(Unlisted));
			Assert.IsTrue(_progression.IsCompleted(Unlisted));
			Assert.IsNull(result.UnlockedLevel);
			Assert.AreEqual(0, _progression.PerfectCount, "only listed levels count");
		}

		#endregion

		#region Best and perfect

		[Test]
		public void FirstCompletion_SetsTheBest_ButIsNotANewBest()
		{
			CompletionResult result = _progression.MarkCompleted(Levels[0], 12, false);

			Assert.IsTrue(result.IsFirstCompletion);
			Assert.IsFalse(result.IsNewBest);
			Assert.IsNull(result.PreviousBest);
			Assert.AreEqual(12, result.Best);
			Assert.AreEqual(12, _progression.Best(Levels[0]));
		}

		[Test]
		public void FewerMoves_IsANewBest()
		{
			_progression.MarkCompleted(Levels[0], 12, false);
			CompletionResult result = _progression.MarkCompleted(Levels[0], 10, false);

			Assert.IsTrue(result.IsNewBest);
			Assert.AreEqual(12, result.PreviousBest);
			Assert.AreEqual(10, result.Best);
			Assert.AreEqual(10, _progression.Best(Levels[0]));
		}

		[TestCase(12)]
		[TestCase(15)]
		public void EqualOrMoreMoves_KeepsTheBest(int moves)
		{
			_progression.MarkCompleted(Levels[0], 12, false);
			CompletionResult result = _progression.MarkCompleted(Levels[0], moves, false);

			Assert.IsFalse(result.IsNewBest);
			Assert.AreEqual(12, result.Best);
			Assert.AreEqual(12, _progression.Best(Levels[0]));
		}

		[Test]
		public void NotCompleted_HasNoBest()
		{
			Assert.IsNull(_progression.Best(Levels[0]));
			Assert.IsFalse(_progression.IsCompleted(Levels[0]));
			Assert.IsFalse(_progression.IsPerfect(Levels[0]));
		}

		[Test]
		public void Perfect_IsKept_OnceEarned()
		{
			CompletionResult first = _progression.MarkCompleted(Levels[0], 7, true);
			CompletionResult second = _progression.MarkCompleted(Levels[0], 20, false);

			Assert.IsTrue(first.IsFirstPerfect);
			Assert.IsFalse(second.IsPerfect);
			Assert.IsFalse(second.IsFirstPerfect);
			Assert.IsTrue(_progression.IsPerfect(Levels[0]));
		}

		[Test]
		public void PerfectCount_CountsListedPerfectLevels()
		{
			_progression.MarkCompleted(Levels[0], 7, true);
			_progression.MarkCompleted(Levels[1], 30, false);
			_progression.MarkCompleted(Levels[2], 8, true);

			Assert.AreEqual(2, _progression.PerfectCount);
			Assert.AreEqual(3, _progression.CompletedCount);
			Assert.AreEqual(4, _progression.LevelCount);
		}

		[Test]
		public void NegativeMoves_Throw()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => _progression.MarkCompleted(Levels[0], -1, false));
		}

		#endregion

		#region Next

		[Test]
		public void NextLevel_IsTheFollowingLevelInTheList()
		{
			Assert.AreEqual(Levels[1], _progression.NextLevel(Levels[0]));
			Assert.IsNull(_progression.NextLevel(Levels[3]));
			Assert.IsNull(_progression.NextLevel(Unlisted));
		}

		[Test]
		public void NextToPlay_IsTheFirstUnlockedUncompletedLevel()
		{
			Assert.AreEqual(Levels[0], _progression.NextToPlay());

			_progression.MarkCompleted(Levels[0], 5, false);
			Assert.AreEqual(Levels[1], _progression.NextToPlay());

			foreach (Guid level in Levels)
			{
				_progression.MarkCompleted(level, 5, false);
			}

			Assert.IsNull(_progression.NextToPlay());
		}

		[Test]
		public void IndexOf_IsThePositionInTheList()
		{
			Assert.AreEqual(2, _progression.IndexOf(Levels[2]));
			Assert.AreEqual(-1, _progression.IndexOf(Unlisted));
		}

		#endregion

		#region Reset and persistence

		[Test]
		public void ResetProgress_ForgetsEverything_AndIsSaved()
		{
			_progression.MarkCompleted(Levels[0], 5, true);
			_progression.UnlockAll();

			_progression.ResetProgress();

			foreach (Progression p in new[] { _progression, Reload() })
			{
				Assert.IsFalse(p.IsUnlocked(Levels[1]));
				Assert.IsFalse(p.IsAllUnlocked);
				Assert.IsNull(p.Best(Levels[0]));
				Assert.AreEqual(0, p.PerfectCount);
			}
		}

		[Test]
		public void Progress_SurvivesAReload_KeyedByGuid()
		{
			_progression.MarkCompleted(Levels[0], 5, true);
			_progression.MarkCompleted(Levels[1], 11, false);

			// A different list order (levels moved) keeps each level's own record.
			Progression reloaded = new Progression(new[] { Levels[1], Levels[0], Levels[2] }, _store);

			Assert.AreEqual(5, reloaded.Best(Levels[0]));
			Assert.IsTrue(reloaded.IsPerfect(Levels[0]));
			Assert.AreEqual(11, reloaded.Best(Levels[1]));
			Assert.IsTrue(reloaded.IsUnlocked(Levels[2]));
		}

		[Test]
		public void EveryChange_SavesOnce_AndRaisesChanged()
		{
			int changed = 0;
			_progression.Changed += () => changed++;

			_progression.MarkCompleted(Levels[0], 5, true);
			_progression.UnlockAll();
			_progression.UnlockAll();
			_progression.ResetProgress();

			Assert.AreEqual(3, _store.SaveCount);
			Assert.AreEqual(3, changed);
		}

		[Test]
		public void Load_SkipsBadRecords()
		{
			ProgressData data = new ProgressData();
			data.levels.Add(null);
			data.levels.Add(new LevelRecord { id = "not a guid", completed = true, best = 3 });
			data.levels.Add(new LevelRecord { id = Levels[0].ToString(), completed = true, best = 4, perfect = true });
			data.levels.Add(new LevelRecord { id = Levels[1].ToString("N"), completed = false, best = 2, perfect = true });

			Progression loaded = new Progression(Levels, new InMemoryProgressStore(data));

			Assert.AreEqual(4, loaded.Best(Levels[0]));
			Assert.IsNull(loaded.Best(Levels[1]));
			Assert.IsFalse(loaded.IsPerfect(Levels[1]), "an uncompleted record can't be perfect");
			Assert.AreEqual(1, loaded.PerfectCount);
		}

		[Test]
		public void DuplicateLevelsInTheList_Throw()
		{
			Assert.Throws<ArgumentException>(() => new Progression(new[] { Levels[0], Levels[0] }, _store));
		}

		[Test]
		public void SetLevelOrder_KeepsRecords()
		{
			_progression.MarkCompleted(Levels[2], 6, false);

			_progression.SetLevelOrder(new[] { Levels[2], Levels[3] });

			Assert.AreEqual(2, _progression.LevelCount);
			Assert.IsTrue(_progression.IsUnlocked(Levels[3]));
		}

		#endregion
	}
}
