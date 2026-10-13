using System;
using System.Collections.Generic;
using Game.UndoSystem;
using Lawnmower.Model;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Presenters.Tests
{
	public class GameFeedbackPresenterTests
	{
		private sealed class Recorder : ISoundEffects, IHaptics
		{
			public readonly List<string> Log = new List<string>();

			public void Play(SoundEffect effect) => Log.Add($"sound {effect}");
			public void LightTap() => Log.Add("haptic tap");
			public void Success() => Log.Add("haptic success");
			public void Error() => Log.Add("haptic error");
		}

		private MovementScheme _schemeBefore;
		private Recorder _out;
		private GameSettings _settings;

		[SetUp]
		public void SetUp()
		{
			_schemeBefore = MovementSchemeSetting.Current;
			_out = new Recorder();
			_settings = new GameSettings(new InMemorySettingsStore());
		}

		[TearDown]
		public void TearDown() => MovementSchemeSetting.Current = _schemeBefore;

		private GameFeedbackPresenter Attach(LevelSimulation sim) => new GameFeedbackPresenter(sim, _out, _out, _settings);

		[Test]
		public void Move_PlaysTheMoveSound_AndALightTap()
		{
			LevelSimulation sim = Create("grass:1 grass:3 grass:3");
			Attach(sim);

			sim.Move(R);

			CollectionAssert.AreEqual(new[] { "sound Move", "haptic tap" }, _out.Log);
		}

		[Test]
		public void Undo_PlaysTheMoveSound_WithoutHaptics()
		{
			LevelSimulation sim = Create("grass:1 grass:3 grass:3", 0, 0, out UndoSystem undo);
			sim.Move(R);
			Attach(sim);

			undo.Undo();

			CollectionAssert.AreEqual(new[] { "sound Move" }, _out.Log);
		}

		[Test]
		public void Bump_PlaysTheBumpSound()
		{
			LevelSimulation sim = Create("grass:1 stone:0 grass:2");
			Attach(sim);

			sim.Move(R);

			CollectionAssert.AreEqual(new[] { "sound Bump" }, _out.Log);
		}

		[Test]
		public void Win_PlaysAfterTheMove_WithTheSuccessHaptic()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2");
			Attach(sim);

			sim.Move(R);
			sim.Move(R);

			CollectionAssert.AreEqual(new[]
			{
				"sound Move", "haptic tap", "sound Move", "haptic tap", "sound Win", "haptic success"
			}, _out.Log);
		}

		[Test]
		public void Fail_PlaysAfterTheMove_WithTheErrorHaptic()
		{
			LevelSimulation sim = Create("grass:1 grass:1 grass:2");
			Attach(sim);

			sim.Move(R);

			CollectionAssert.AreEqual(new[] { "sound Move", "haptic tap", "sound Fail", "haptic error" }, _out.Log);
		}

		[Test]
		public void UndoingAFail_PlaysNoFailSound()
		{
			LevelSimulation sim = Create("grass:1 grass:1 grass:2", 0, 0, out UndoSystem undo);
			sim.Move(R);
			Attach(sim);

			undo.Undo();

			CollectionAssert.AreEqual(new[] { "sound Move" }, _out.Log);
		}

		[Test]
		public void HapticsOff_KeepsTheSounds()
		{
			LevelSimulation sim = Create("grass:1 grass:2 grass:2");
			Attach(sim);
			_settings.Haptics = false;

			sim.Move(R);
			sim.Move(R);

			CollectionAssert.AreEqual(new[] { "sound Move", "sound Move", "sound Win" }, _out.Log);
		}

		[Test]
		public void Dispose_StopsListening()
		{
			LevelSimulation sim = Create("grass:1 grass:3 grass:3");
			GameFeedbackPresenter feedback = Attach(sim);

			feedback.Dispose();
			feedback.Dispose();
			sim.Move(R);

			Assert.IsEmpty(_out.Log);
		}

		[Test]
		public void NullArguments_Throw()
		{
			LevelSimulation sim = Create("grass:1 grass:2");

			Assert.Throws<ArgumentNullException>(() => new GameFeedbackPresenter(null, _out, _out, _settings));
			Assert.Throws<ArgumentNullException>(() => new GameFeedbackPresenter(sim, null, _out, _settings));
			Assert.Throws<ArgumentNullException>(() => new GameFeedbackPresenter(sim, _out, null, _settings));
			Assert.Throws<ArgumentNullException>(() => new GameFeedbackPresenter(sim, _out, _out, null));
		}
	}
}
