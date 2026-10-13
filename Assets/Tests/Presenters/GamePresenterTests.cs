using System;
using System.Linq;
using Game.Core;
using Lawnmower.Model;
using Lawnmower.Model.Tests;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Presenters.Tests
{
	public class GamePresenterTests
	{
		private ViewLog _log;
		private FakeBoard _board;
		private FakeMower _mower;
		private FakeScreen _screen;
		private FakeNavigator _navigator;
		private FakeProgress _progress;
		private FakeClock _clock;
		private FakeInput _input;

		[SetUp]
		public void SetUp()
		{
			_log = new ViewLog();
			_board = new FakeBoard(_log);
			_mower = new FakeMower(_log);
			_screen = new FakeScreen(_log);
			_navigator = new FakeNavigator(_log);
			_progress = new FakeProgress();
			_clock = new FakeClock();
			_input = new FakeInput();
		}

		private GamePresenter Create(string rows, int startX = 0, int startY = 0, int par = 0, bool begin = true)
		{
			GamePresenter presenter = new GamePresenter(
				new GameSessionSetup(LevelText.Parse(rows, V(startX, startY)), par),
				_board, _mower, _screen, _navigator, _progress, _clock);
			presenter.AttachInput(_input);
			presenter.Start();

			if (begin)
			{
				presenter.BeginPlay();
			}

			return presenter;
		}

		#region Start

		[Test]
		public void Start_ShowsBoardMowerAndProgress()
		{
			Create("grass:1 grass:2 grass:2", par: 2, begin: false);

			CollectionAssert.AreEqual(new[] { "board show 3x1", "mower show [0, 0]", "progress 0/2" }, _log.Lines);
		}

		[Test]
		public void Start_GivesEachSessionFreshTiles()
		{
			LevelDefinition definition = LevelText.Parse("grass:1 grass:2 grass:2", V(0, 0));
			GamePresenter first = new GamePresenter(new GameSessionSetup(definition, 0), _board, _mower, _screen, _navigator, _progress, _clock);
			first.Start();
			first.BeginPlay();
			first.RequestMove(R);

			GamePresenter second = new GamePresenter(new GameSessionSetup(definition, 0), _board, _mower, _screen, _navigator, _progress, _clock);
			second.Start();

			Assert.AreEqual(2, Height(second.Simulation, 1, 0));
			Assert.AreNotSame(first.Simulation.Level, second.Simulation.Level);
		}

		[Test]
		public void Start_Twice_Throws()
		{
			GamePresenter presenter = Create("grass:1 grass:2");
			Assert.Throws<InvalidOperationException>(presenter.Start);
		}

		[Test]
		public void BeginPlay_BeforeStart_Throws()
		{
			GamePresenter presenter = new GamePresenter(
				new GameSessionSetup(LevelText.Parse("grass:1 grass:2", V(0, 0)), 0),
				_board, _mower, _screen, _navigator, _progress, _clock);

			Assert.Throws<InvalidOperationException>(presenter.BeginPlay);
		}

		[Test]
		public void AlreadyRuinedLevel_ShowsFailedAtStart()
		{
			Create("grass:2 grass:0");
			Assert.IsTrue(_screen.IsFailedShown);
		}

		#endregion

		#region Input

		[Test]
		public void Input_BeforeBeginPlay_IsIgnored()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2", begin: false);

			_input.Press(R);

			Assert.AreEqual(V(0, 0), presenter.Simulation.MowerPosition.Value);
			Assert.AreEqual(0, _log.Count("mower play"));
		}

		[Test]
		public void Input_MovesTheSimulationAndPlaysTheMove()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2", par: 2);

			_input.Press(R);

			Assert.AreEqual(V(1, 0), presenter.Simulation.MowerPosition.Value);
			CollectionAssert.AreEqual(new[] { "mower play Step [0, 0]->[1, 0]", "progress 1/2" }, _log.Lines.Skip(3));
		}

		[Test]
		public void NonUnitDirection_IsIgnored()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");

			Assert.DoesNotThrow(() => presenter.RequestMove(V(2, 0)));
			Assert.DoesNotThrow(() => presenter.RequestMove(V(1, 1)));
			Assert.AreEqual(V(0, 0), presenter.Simulation.MowerPosition.Value);
		}

		[Test]
		public void DetachedInput_NoLongerMoves()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			presenter.DetachInput(_input);

			_input.Press(R);

			Assert.AreEqual(V(0, 0), presenter.Simulation.MowerPosition.Value);
			Assert.IsFalse(_input.HasListeners);
		}

		[Test]
		public void Bump_PlaysABump_AndIsNotAMove()
		{
			GamePresenter presenter = Create("grass:1 stone:0 grass:2");

			_input.Press(R);

			Assert.AreEqual(1, _log.Count("mower bump"));
			Assert.AreEqual(0, presenter.Stats.Moves);
			Assert.AreEqual(0, _log.Count("mower play"));
		}

		[Test]
		public void SpringMove_PlaysTheWholePathInOneCall()
		{
			Create("grass:1 spring:1,0 grass:2");

			_input.Press(R);

			Assert.AreEqual("mower play Step [0, 0]->[1, 0] Jump [1, 0]->[2, 0]", _log.Lines.Single(l => l.StartsWith("mower play")));
		}

		#endregion

		#region Undo and redo

		[Test]
		public void Undo_RewindsTheMove_AndTakesAMoveOff()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			_input.Press(R);

			_screen.PressUndo();

			Assert.AreEqual(V(0, 0), presenter.Simulation.MowerPosition.Value);
			Assert.AreEqual(1, _log.Count("mower rewind [1, 0]->[0, 0]"));
			Assert.AreEqual(0, presenter.Stats.Moves);
			Assert.AreEqual(1, presenter.Stats.Undos);
			Assert.AreEqual(0, _screen.Moves);
		}

		[Test]
		public void Undo_WithNothingToUndo_CountsNothing()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");

			presenter.Undo();

			Assert.AreEqual(0, presenter.Stats.Undos);
		}

		[Test]
		public void Redo_ReplaysTheMove()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			_input.Press(R);
			_screen.PressUndo();

			_screen.PressRedo();

			Assert.AreEqual(V(1, 0), presenter.Simulation.MowerPosition.Value);
			Assert.AreEqual(2, _log.Count("mower play"));
			Assert.AreEqual(1, presenter.Stats.Moves);
		}

		[Test]
		public void CommandsFromInsideASimulationEvent_AreQueued_NotReentered()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2 grass:2");
			bool undoneOnce = false;
			_mower.OnPlayMove = move =>
			{
				if (!undoneOnce)
				{
					undoneOnce = true;
					presenter.Undo();
					presenter.RequestMove(R);
				}
			};

			Assert.DoesNotThrow(() => _input.Press(R));

			// Move R, then (queued) undo it, then (queued) move R again.
			Assert.AreEqual(V(1, 0), presenter.Simulation.MowerPosition.Value);
			Assert.AreEqual(1, presenter.Stats.Moves);
			Assert.AreEqual(1, presenter.Stats.Undos);
		}

		#endregion

		#region Completed

		[Test]
		public void Completing_RecordsTheAttempt_ThenShowsCompletedWithTheFinalMoveCount()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2", par: 2);
			_clock.Now = 10;
			presenter.BeginPlay();
			_clock.Now = 15;

			_input.Press(R);
			_input.Press(R);

			Assert.AreEqual(1, _progress.Records.Count);
			Assert.AreEqual(LevelText.DefaultId, _progress.Records[0].id);
			Assert.AreEqual(2, _progress.Records[0].stats.Moves);
			Assert.AreEqual(2, _progress.Records[0].par);
			Assert.IsTrue(_screen.Completed.HasValue);
			Assert.AreEqual(2, _screen.Completed.Value.Moves);
			Assert.IsTrue(_screen.Completed.Value.IsPerfect);
			Assert.IsTrue(_screen.Completed.Value.HasNextLevel);
			Assert.Less(_log.Lines.IndexOf("progress 2/2"), _log.Lines.FindIndex(l => l.StartsWith("completed")),
				"the counter updates before the card");
		}

		[Test]
		public void Duration_StartsAtBeginPlay()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2", begin: false);
			_clock.Now = 3;
			presenter.BeginPlay();
			_clock.Now = 10.5;

			Assert.AreEqual(7.5, presenter.Stats.Duration, 1e-9);
		}

		[Test]
		public void UndoTakesAMoveOff_ForTheCompletedCount()
		{
			Create("grass:1 grass:2 grass:2", par: 2);
			_input.Press(R);
			_screen.PressUndo();
			_input.Press(R);
			_input.Press(R);

			Assert.AreEqual(2, _screen.Completed.Value.Moves);
			Assert.IsTrue(_screen.Completed.Value.IsPerfect);
		}

		[Test]
		public void CompletingOverPar_IsNotPerfect()
		{
			Create("grass:1 grass:2 grass:2", par: 1);
			_input.Press(R);
			_input.Press(R);

			Assert.IsFalse(_screen.Completed.Value.IsPerfect);
		}

		[Test]
		public void UnknownPar_IsNeverPerfect()
		{
			Create("grass:1 grass:2 grass:2", par: 0);
			_input.Press(R);
			_input.Press(R);

			Assert.IsFalse(_screen.Completed.Value.IsPerfect);
		}

		[Test]
		public void UndoingTheCompletingMove_HidesCompleted()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			_input.Press(R);
			_input.Press(R);

			presenter.Undo();

			Assert.IsFalse(_screen.Completed.HasValue);
			Assert.AreEqual(1, _log.Count("hide completed"));
		}

		[Test]
		public void Next_LoadsTheNextLevel_OrLevelSelectAfterTheLast()
		{
			Create("grass:1 grass:2");
			_screen.PressNext();
			_navigator.HasNextLevel = false;
			_screen.PressNext();

			CollectionAssert.AreEqual(new[] { "nav next", "nav level select" }, _log.Lines.FindAll(l => l.StartsWith("nav")));
		}

		#endregion

		#region Failed

		[Test]
		public void Failing_ShowsFailed_AndUndoLastMoveResumesPlay()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			_input.Press(R);
			_input.Press(L);
			Assert.AreEqual(LevelStatus.Failed, presenter.Simulation.Status);
			Assert.IsTrue(_screen.IsFailedShown);

			_screen.PressUndo();

			Assert.IsFalse(_screen.IsFailedShown);
			Assert.AreEqual(LevelStatus.Playing, presenter.Simulation.Status);
			_input.Press(R);
			Assert.AreEqual(V(2, 0), presenter.Simulation.MowerPosition.Value);
			Assert.AreEqual(LevelStatus.Completed, presenter.Simulation.Status);
		}

		[Test]
		public void WhileFailed_MovesAndRedoAreIgnored()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			_input.Press(R);
			_input.Press(L);

			_input.Press(R);
			presenter.Redo();

			Assert.AreEqual(V(0, 0), presenter.Simulation.MowerPosition.Value);
			Assert.AreEqual(LevelStatus.Failed, presenter.Simulation.Status);
		}

		[Test]
		public void Restart_AndLevelSelect_GoToTheNavigator()
		{
			Create("grass:1 grass:2");
			_screen.PressRestart();
			_screen.PressLevelSelect();

			CollectionAssert.AreEqual(new[] { "nav restart", "nav level select" }, _log.Lines.FindAll(l => l.StartsWith("nav")));
		}

		#endregion

		#region Dispose

		[Test]
		public void Dispose_DetachesEverything_AndLeavesTheViewsToTheOwner()
		{
			GamePresenter presenter = Create("grass:1 grass:2 grass:2");
			_log.Lines.Clear();

			presenter.Dispose();
			_input.Press(R);
			_screen.PressUndo();
			_screen.PressRestart();

			Assert.IsFalse(_input.HasListeners);
			Assert.IsFalse(presenter.IsAcceptingInput);
			CollectionAssert.IsEmpty(_log.Lines);
			Assert.Throws<ObjectDisposedException>(presenter.BeginPlay);
		}

		#endregion
	}
}
