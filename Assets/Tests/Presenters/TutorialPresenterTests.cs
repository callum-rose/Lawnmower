using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class TutorialPresenterTests
	{
		private sealed class FakeTutorialView : ITutorialView
		{
			public readonly List<string> Log = new List<string>();
			public event Action Dismissed;
			public void ShowStep(TutorialStep step) => Log.Add(step.ToString());
			public void Hide() => Log.Add("hide");
			public void Dismiss() => Dismissed?.Invoke();
		}

		private MovementScheme _schemeBefore;
		private FakeTutorialView _view;
		private FakeInput _input;
		private int _seen;

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			MovementSchemeSetting.Current = MovementScheme.Swipe;
			_view = new FakeTutorialView();
			_input = new FakeInput();
			_seen = 0;
			TutorialLauncher.CancelRequest();
		}

		[TearDown]
		public void TearDown()
		{
			MovementSchemeSetting.Current = _schemeBefore;
			TutorialLauncher.CancelRequest();
		}

		private TutorialPresenter Create() => new TutorialPresenter(_view, _input, () => _seen++);

		[Test]
		public void Swipe_ThenMowTheRest_ThenDismissed_MarksSeen()
		{
			TutorialPresenter tutorial = Create();
			bool finished = false;
			tutorial.Finished += () => finished = true;
			tutorial.Start();
			Assert.AreEqual(TutorialStep.Swipe, tutorial.Step);

			_input.Press(GridVector.Up);
			Assert.AreEqual(TutorialStep.MowTheRest, tutorial.Step);

			_view.Dismiss();
			Assert.IsFalse(tutorial.IsRunning);
			Assert.IsTrue(finished);
			Assert.AreEqual(1, _seen);
			CollectionAssert.AreEqual(new[] { "Swipe", "MowTheRest", "hide" }, _view.Log);
		}

		[Test]
		public void ArrowsScheme_AsksToTapTheArrows()
		{
			MovementSchemeSetting.Current = MovementScheme.Arrows;
			TutorialPresenter tutorial = Create();
			tutorial.Start();
			Assert.AreEqual(TutorialStep.TapArrows, tutorial.Step);
		}

		[Test]
		public void ChangingTheScheme_UpdatesTheMoveStep()
		{
			TutorialPresenter tutorial = Create();
			tutorial.Start();
			MovementSchemeSetting.Current = MovementScheme.Arrows;
			Assert.AreEqual(TutorialStep.TapArrows, tutorial.Step);
		}

		[Test]
		public void DismissingTheMoveStep_DoesNothing()
		{
			TutorialPresenter tutorial = Create();
			tutorial.Start();
			_view.Dismiss();
			Assert.AreEqual(TutorialStep.Swipe, tutorial.Step);
			Assert.AreEqual(0, _seen);
		}

		[Test]
		public void Dispose_PartWay_DoesNotMarkSeen()
		{
			TutorialPresenter tutorial = Create();
			tutorial.Start();
			tutorial.Dispose();
			_input.Press(GridVector.Up);
			Assert.AreEqual(0, _seen);
			Assert.IsFalse(_input.HasListeners);
			Assert.AreEqual("hide", _view.Log[_view.Log.Count - 1]);
		}

		[Test]
		public void Launcher_ShowsOnTheFirstLevelUntilSeen()
		{
			Assert.IsTrue(TutorialLauncher.ShouldShow(isFirstLevel: true, hasSeenTutorial: false));
			Assert.IsFalse(TutorialLauncher.ShouldShow(isFirstLevel: true, hasSeenTutorial: true));
			Assert.IsFalse(TutorialLauncher.ShouldShow(isFirstLevel: false, hasSeenTutorial: false));
		}

		[Test]
		public void Launcher_Request_ShowsOnTheNextLoadOnly()
		{
			TutorialLauncher.Request();
			Assert.IsTrue(TutorialLauncher.IsRequested);
			Assert.IsTrue(TutorialLauncher.ShouldShow(isFirstLevel: false, hasSeenTutorial: true));
			Assert.IsFalse(TutorialLauncher.ShouldShow(isFirstLevel: false, hasSeenTutorial: true));
		}
	}
}
