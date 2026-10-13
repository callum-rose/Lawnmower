using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class AttemptTrackerTests
	{
		private FakeClock _clock;
		private AttemptTracker _attempt;

		[SetUp]
		public void SetUp()
		{
			_clock = new FakeClock { Now = 100 };
			_attempt = new AttemptTracker(_clock);
		}

		[Test]
		public void ClockDoesNotRun_BeforeStart()
		{
			_clock.Now = 150;

			Assert.AreEqual(0, _attempt.Elapsed);
			Assert.IsFalse(_attempt.IsClockRunning);
		}

		[Test]
		public void Elapsed_CountsFromStart()
		{
			_attempt.Start();
			_clock.Now = 112.5;

			Assert.AreEqual(12.5, _attempt.Elapsed, 1e-9);
		}

		[Test]
		public void Pause_StopsTheClock_AndResumeRestartsIt()
		{
			_attempt.Start();
			_clock.Now = 110;
			_attempt.Pause();
			_clock.Now = 500;

			Assert.AreEqual(10, _attempt.Elapsed, 1e-9);
			Assert.IsTrue(_attempt.IsPaused);

			_attempt.Resume();
			_clock.Now = 505;

			Assert.AreEqual(15, _attempt.Elapsed, 1e-9);
		}

		[Test]
		public void PausingTwice_StillCountsOnce()
		{
			_attempt.Start();
			_clock.Now = 110;
			_attempt.Pause();
			_clock.Now = 120;
			_attempt.Pause();
			_attempt.Resume();
			_attempt.Resume();
			_clock.Now = 125;

			Assert.AreEqual(15, _attempt.Elapsed, 1e-9);
		}

		[Test]
		public void PausedBeforeStart_StartsPaused()
		{
			_attempt.Pause();
			_attempt.Start();
			_clock.Now = 130;

			Assert.AreEqual(0, _attempt.Elapsed);

			_attempt.Resume();
			_clock.Now = 131;

			Assert.AreEqual(1, _attempt.Elapsed, 1e-9);
		}

		[Test]
		public void Finished_FreezesTheClock_UntilUnfinished()
		{
			_attempt.Start();
			_clock.Now = 120;
			_attempt.SetFinished(true);
			_clock.Now = 200;

			Assert.AreEqual(20, _attempt.Elapsed, 1e-9);

			_attempt.SetFinished(false);
			_clock.Now = 205;

			Assert.AreEqual(25, _attempt.Elapsed, 1e-9);
		}

		[Test]
		public void Start_OnlyCountsOnce()
		{
			_attempt.Start();
			_clock.Now = 110;
			_attempt.Start();
			_clock.Now = 120;

			Assert.AreEqual(20, _attempt.Elapsed, 1e-9);
		}

		[Test]
		public void Moves_GoUpAndDown_NeverBelowZero()
		{
			_attempt.CountMove();
			_attempt.CountMove();
			_attempt.CountMoveUndone();
			_attempt.CountUndo();

			Assert.AreEqual(1, _attempt.Moves);
			Assert.AreEqual(1, _attempt.Undos);

			_attempt.CountMoveUndone();
			_attempt.CountMoveUndone();

			Assert.AreEqual(0, _attempt.Moves);
		}
	}
}
