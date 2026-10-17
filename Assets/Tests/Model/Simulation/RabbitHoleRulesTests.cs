using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// P -> A ~> X -> Landing: the mower at P steps into hole A, comes out of its partner X and lands one tile
	/// beyond it, still heading the way it went in.
	/// </summary>
	public class RabbitHoleRulesTests
	{
		private const string Basic = "grass:1 hole:0 wood hole:0 grass:2";
		private static readonly GridVector P = V(0, 0), A = V(1, 0), X = V(3, 0), Landing = V(4, 0);

		[Test]
		public void BasicBurrow()
		{
			LevelSimulation sim = Create(Basic);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			CollectionAssert.AreEqual(new[] { P, A, Landing }, result.Path);
			Assert.AreEqual(2, result.Segments.Count);
			Assert.AreEqual((P, A, MovementKind.Step), (result.Segments[0].From, result.Segments[0].To, result.Segments[0].Kind));
			Assert.AreEqual((A, Landing, MovementKind.Burrow), (result.Segments[1].From, result.Segments[1].To, result.Segments[1].Kind));
			Assert.AreEqual(R, result.Segments[1].Direction, "a burrow's direction is the heading, not the jump");
			Assert.AreEqual(X, result.Segments[1].ExitHole);
			Assert.AreEqual(Landing, sim.MowerPosition.Value);
			Assert.IsNull(result.BumpedAt);
			Assert.AreEqual(1, Height(sim, 4, 0), "the landing is mowed");
			Assert.AreEqual(LevelStatus.Completed, sim.Status);
		}

		[Test]
		public void Burrow_IsOneUndoStep()
		{
			LevelSimulation sim = Create(Basic, 0, 0, out UndoSystem undo);
			SimulationSnapshot initial = sim.Snapshot();

			sim.Move(R);
			Assert.IsTrue(undo.Undo());

			Assert.AreEqual(P, sim.MowerPosition.Value, "back at P, not in a hole");
			Assert.AreEqual(initial, sim.Snapshot());
			Assert.IsFalse(undo.Undo());
			Assert.IsTrue(undo.Redo());
			Assert.AreEqual(Landing, sim.MowerPosition.Value);
		}

		[Test]
		public void Burrow_TellsTheTilesTheHeading()
		{
			LevelSimulation sim = Create(Basic);
			SimulationRecorder events = new SimulationRecorder(sim);

			sim.Move(R);

			CollectionAssert.AreEqual(new[] { "onto 1,0 R", "onto 4,0 R" }, events.Log.FindAll(l => l.StartsWith("onto")));
		}

		private static TestCaseData Approach(int x, int y, GridVector direction, int landingX, int landingY, string name) =>
			new TestCaseData(x, y, direction, V(landingX, landingY)).SetName(name);

		private static TestCaseData[] Approaches =
		{
			// Holes at (1,1) and (3,1) on a 5x3 lawn.
			Approach(0, 1, R, 4, 1, "InFromTheLeft_OutToTheRight"),
			Approach(2, 1, L, 2, 1, "InFromTheRight_OutToTheLeft"),
			Approach(1, 0, U, 3, 2, "InFromBelow_OutUpwards"),
			Approach(1, 2, D, 3, 0, "InFromAbove_OutDownwards")
		};

		[TestCaseSource(nameof(Approaches))]
		public void TheWayIn_IsTheWayOut(int x, int y, GridVector direction, GridVector landing)
		{
			LevelSimulation sim = Create("grass:3 grass:3 grass:3 grass:3 grass:3 / " +
			                             "grass:3 hole:0 grass:3 hole:0 grass:3 / " +
			                             "grass:3 grass:3 grass:3 grass:3 grass:3", x, y);

			MoveResult result = sim.Move(direction);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(landing, result.To);
			Assert.AreEqual(2, Height(sim, landing.x, landing.y));
		}

		[Test]
		public void HolesWorkBothWays()
		{
			LevelSimulation sim = Create("grass:2 hole:0 grass:3 hole:0 grass:3", 0, 0);

			Assert.AreEqual(V(4, 0), sim.Move(R).To);
			Assert.AreEqual(V(0, 0), sim.Move(L).To, "into the right hole heading left, out of the left one");
			Assert.AreEqual(V(4, 0), sim.Move(R).To, "into the left hole heading right, out of the right one");
			Assert.AreEqual(3, Height(sim, 2, 0), "the lawn between the holes is never driven over");
		}

		[TestCase("stone:0")]
		[TestCase("water")]
		[TestCase("empty")]
		public void BlockedExit_BouncesBack_AndBumpsTheExit(string obstacle)
		{
			LevelSimulation sim = Create($"grass:1 hole:0 grass:2 hole:0 {obstacle}", 0, 0, out UndoSystem undo);
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
			Assert.AreEqual(P, result.To);
			Assert.AreEqual(P, sim.MowerPosition.Value);
			CollectionAssert.IsEmpty(result.Segments);
			Assert.AreEqual(V(4, 0), result.BumpedAt);
			Assert.AreEqual(2, result.AttemptedSegments.Count);
			Assert.AreEqual((A, V(4, 0), MovementKind.Burrow),
				(result.AttemptedSegments[1].From, result.AttemptedSegments[1].To, result.AttemptedSegments[1].Kind));
			Assert.AreEqual(X, result.AttemptedSegments[1].ExitHole);
			CollectionAssert.AreEqual(new[] { "bump 4,0 R", "bounced R at 4,0" }, events.Log);
			Assert.AreEqual(before, sim.Snapshot());
			Assert.IsFalse(undo.Undo(), "a bounce isn't an undo step");
		}

		[Test]
		public void ExitOffTheLevel_Bounces()
		{
			LevelSimulation sim = Create("grass:1 hole:0 grass:2 hole:0", 0, 0, out UndoSystem undo);
			SimulationRecorder events = new SimulationRecorder(sim);

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
			Assert.AreEqual(P, sim.MowerPosition.Value);
			Assert.IsNull(result.BumpedAt);
			CollectionAssert.AreEqual(new[] { "bounced R" }, events.Log);
			Assert.IsFalse(undo.Undo());
		}

		[TestCase("hole:1 grass:2 hole:1", TestName = "ExitOntoAnotherPairsHole_Bounces")]
		[TestCase("hole:0 grass:2", TestName = "ExitOntoAThirdHoleOfThePair_DoesNotLink")]
		public void ExitOntoAHole(string rest)
		{
			LevelSimulation sim = Create($"grass:1 hole:0 grass:2 hole:0 {rest}");

			MoveResult result = sim.Move(R);

			if (rest.StartsWith("hole:1"))
			{
				Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
				Assert.IsNull(result.BumpedAt, "a hole isn't bumped");
			}
			else
			{
				Assert.AreEqual(MoveOutcome.Moved, result.Outcome, "three holes in a pair aren't linked, so they're plain tiles");
				Assert.AreEqual(A, result.To);
			}

			Assert.AreEqual(2, Height(sim, 2, 0));
		}

		[Test]
		public void BounceFromASpring_UndoesTheWholeMove()
		{
			LevelSimulation sim = Create("grass:1 spring:1,0 hole:0 grass:2 hole:0 stone:0");

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Bounced, result.Outcome);
			Assert.AreEqual(P, sim.MowerPosition.Value);
			CollectionAssert.AreEqual(new[] { MovementKind.Step, MovementKind.Jump, MovementKind.Burrow },
				new[] { result.AttemptedSegments[0].Kind, result.AttemptedSegments[1].Kind, result.AttemptedSegments[2].Kind });
		}

		[Test]
		public void LandingOnASpring_JumpsOn()
		{
			LevelSimulation sim = Create("grass:1 hole:0 grass:2 hole:0 spring:0,1 / grass:2 grass:2 grass:2 grass:2 grass:2");

			MoveResult result = sim.Move(R);

			CollectionAssert.AreEqual(new[] { P, A, V(4, 0), V(4, 1) }, result.Path);
			Assert.AreEqual(1, Height(sim, 4, 1));
		}

		[Test]
		public void LandingRuinsATile_Fails()
		{
			LevelSimulation sim = Create("grass:2 hole:0 grass:2 hole:0 grass:1");

			Assert.AreEqual(LevelStatus.Failed, sim.Move(R).StatusAfter);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);
		}

		[Test]
		public void UnpairedHole_IsJustATile()
		{
			LevelSimulation sim = Create("grass:1 hole:0 grass:2");

			MoveResult result = sim.Move(R);

			Assert.AreEqual(MoveOutcome.Moved, result.Outcome);
			Assert.AreEqual(A, sim.MowerPosition.Value);
			Assert.AreEqual(V(2, 0), sim.Move(R).To);
		}

		[Test]
		public void Level_LinksEachHoleToTheOtherOfItsPair()
		{
			Level level = Level.FromDefinition(LevelText.Parse("hole:0 hole:1 grass:2 hole:1 hole:0", P));

			Assert.AreEqual(V(4, 0), ((RabbitHoleTile)level.GetTile(0, 0)).Partner);
			Assert.AreEqual(V(0, 0), ((RabbitHoleTile)level.GetTile(4, 0)).Partner);
			Assert.AreEqual(V(3, 0), ((RabbitHoleTile)level.GetTile(1, 0)).Partner);
			Assert.AreEqual(V(1, 0), ((RabbitHoleTile)level.GetTile(3, 0)).Partner);
			Assert.AreEqual(V(4, 0), ((RabbitHoleTile)level.Clone().GetTile(0, 0)).Partner, "a clone is linked too");
		}

		[Test]
		public void Preview_ShowsABounce_WithoutChangingAnything()
		{
			LevelSimulation sim = Create("grass:1 hole:0 grass:2 hole:0 stone:0");
			SimulationSnapshot before = sim.Snapshot();
			SimulationRecorder events = new SimulationRecorder(sim);

			Assert.AreEqual(MoveOutcome.Bounced, sim.Preview(R).Outcome);
			Assert.AreEqual(before, sim.Snapshot());
			CollectionAssert.IsEmpty(events.Log);
		}

		[Test]
		public void HoleTile_NeverChangesState()
		{
			LevelSimulation sim = Create(Basic, 0, 0, out UndoSystem undo);
			TileSpec before = Spec(sim, 1, 0);

			sim.Move(R);
			Assert.AreEqual(before, Spec(sim, 1, 0));
			undo.Undo();
			Assert.AreEqual(before, Spec(sim, 1, 0));
		}
	}
}
