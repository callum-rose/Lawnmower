using System.Collections.Generic;
using System.Linq;
using Game.UndoSystem;
using NUnit.Framework;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// For random levels and random inputs (which naturally include bumps, out-of-bounds moves,
	/// rejected moves and moves after Failed): undo-all and redo-all retrace every state exactly.
	/// </summary>
	public class UndoRoundTripPropertyTests
	{
		private static IEnumerable<int> Seeds() => Enumerable.Range(1, 50);

		[TestCaseSource(nameof(Seeds))]
		public void UndoAll_ThenRedoAll_RetracesEveryState(int seed)
		{
			LevelDefinition def = RandomLevels.Generate(seed);
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Level.FromDefinition(def), undo);

			UndoRoundTrip.Check(sim, undo, RandomLevels.Moves(seed, 60));

			Assert.AreEqual(def, Level.FromDefinition(def).ToDefinition(), "the definition is untouched");
		}

		[Test]
		public void Generator_IsDeterministic_AndCoversTheMechanics()
		{
			Assert.AreEqual(RandomLevels.Generate(7), RandomLevels.Generate(7));

			List<LevelDefinition> levels = Seeds().Select(RandomLevels.Generate).ToList();
			HashSet<TileKind> kinds = new HashSet<TileKind>(levels.SelectMany(l => l.Tiles).Select(t => t.Kind));
			CollectionAssert.IsSupersetOf(kinds, new[] { TileKind.Grass, TileKind.Stone, TileKind.Water, TileKind.Empty, TileKind.Wood, TileKind.Spring, TileKind.RabbitHole });

			HashSet<MoveOutcome> outcomes = new HashSet<MoveOutcome>();
			bool movedAfterFailed = false;
			foreach (int seed in Seeds())
			{
				LevelSimulation sim = new LevelSimulation(Level.FromDefinition(RandomLevels.Generate(seed)), new UndoSystem());
				foreach (var move in RandomLevels.Moves(seed, 60))
				{
					bool failed = sim.Status == LevelStatus.Failed;
					MoveResult r = sim.Move(move);
					outcomes.Add(r.Outcome);
					movedAfterFailed |= failed && r.Outcome == MoveOutcome.Moved;
				}
			}

			CollectionAssert.IsSupersetOf(outcomes, new[] { MoveOutcome.Moved, MoveOutcome.Bumped, MoveOutcome.OutOfBounds });
			Assert.IsTrue(movedAfterFailed, "some sequence keeps moving after Failed");
		}
	}
}
