using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.UndoSystem;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	/// <summary>The ground the solver (S1) stands on: determinism, isolation, Snapshot and Clone.</summary>
	public class DeterminismAndStateTests
	{
		private const string Mixed =
			"grass:1 grass:2 stone:0 grass:3 / wood spring:0,2 grass:2 water / grass:2 empty grass:3 spring:-3,0 / grass:3 grass:2 wood grass:1";

		private static LevelDefinition MixedDefinition() => LevelText.Parse(Mixed, V(0, 0));

		private static (List<string> results, List<SimulationSnapshot> snapshots) Run(LevelDefinition def, int seed, int count)
		{
			LevelSimulation sim = Create(def, new UndoSystem());
			Random random = new Random(seed);
			List<string> results = new List<string>();
			List<SimulationSnapshot> snapshots = new List<SimulationSnapshot>();

			for (int i = 0; i < count; i++)
			{
				MoveResult r = sim.Move(Directions[random.Next(4)]);
				results.Add($"{r} bump={r.BumpedAt} segs={string.Join(",", r.Segments)}");
				snapshots.Add(sim.Snapshot());
			}

			return (results, snapshots);
		}

		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void SameInputs_GiveIdenticalResultsAndSnapshots(int seed)
		{
			var a = Run(MixedDefinition(), seed, 200);
			var b = Run(MixedDefinition(), seed, 200);

			CollectionAssert.AreEqual(a.results, b.results);
			CollectionAssert.AreEqual(a.snapshots, b.snapshots);
		}

		[Test]
		public void NoHiddenStaticState_AcrossRuns()
		{
			var first = Run(MixedDefinition(), 1, 200);
			Run(MixedDefinition(), 2, 200);
			Run(RandomLevels.Generate(5), 5, 200);
			var again = Run(MixedDefinition(), 1, 200);

			CollectionAssert.AreEqual(first.results, again.results);
			CollectionAssert.AreEqual(first.snapshots, again.snapshots);
		}

		[Test]
		public void TwoSimulationsOnOneDefinition_DoNotAffectEachOther()
		{
			LevelDefinition def = MixedDefinition();
			LevelDefinition copy = MixedDefinition();
			LevelSimulation a = Create(def, new UndoSystem());
			LevelSimulation b = Create(def, new UndoSystem());
			LevelSimulation soloA = Create(def, new UndoSystem());
			LevelSimulation soloB = Create(def, new UndoSystem());
			IReadOnlyList<GridVector> movesA = MoveNotation.Parse("RDURLLUUUR");
			IReadOnlyList<GridVector> movesB = MoveNotation.Parse("UURRDDLLUR");

			for (int i = 0; i < movesA.Count; i++)
			{
				a.Move(movesA[i]);
				b.Move(movesB[i]);
			}

			foreach (GridVector m in movesA) soloA.Move(m);
			foreach (GridVector m in movesB) soloB.Move(m);

			Assert.AreEqual(soloA.Snapshot(), a.Snapshot());
			Assert.AreEqual(soloB.Snapshot(), b.Snapshot());
			Assert.AreEqual(copy, def, "the definition is unchanged");
		}

		[Test]
		public void Snapshot_FreshSimulationsAreEqual_WithEqualHashes()
		{
			SimulationSnapshot a = Create(MixedDefinition(), new UndoSystem()).Snapshot();
			SimulationSnapshot b = Create(MixedDefinition(), new UndoSystem()).Snapshot();

			Assert.AreEqual(a, b);
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
			Assert.AreEqual(16, a.TileCount);
			Assert.AreEqual(V(0, 0), a.MowerPosition);
			Assert.AreEqual(LevelStatus.Playing, a.Status);
		}

		[Test]
		public void Snapshot_ChangesOnAMow_NotOnABump()
		{
			LevelSimulation sim = Create("grass:1 grass:2 stone:0 / grass:2 grass:2 grass:2");
			SimulationSnapshot start = sim.Snapshot();

			sim.Move(R);
			SimulationSnapshot afterMow = sim.Snapshot();
			Assert.AreNotEqual(start, afterMow);

			Assert.AreEqual(MoveOutcome.Bumped, sim.Move(R).Outcome);
			Assert.AreEqual(afterMow, sim.Snapshot());
		}

		[Test]
		public void Snapshot_ExcludesHistory()
		{
			LevelSimulation sim = Create("wood wood grass:2", 0, 0, out UndoSystem undo);
			SimulationSnapshot start = sim.Snapshot();

			sim.Move(R);
			sim.Move(L);

			Assert.AreEqual(start, sim.Snapshot(), "same state, even though the undo depth is 2");
			Assert.AreEqual(2, UndoAll(undo));
		}

		[Test]
		public void Snapshot_IsImmutable()
		{
			LevelSimulation sim = Create("grass:1 grass:3 grass:2");
			SimulationSnapshot before = sim.Snapshot();
			TileSpec tileBefore = before.GetTile(1);

			sim.Move(R);

			Assert.AreEqual(V(0, 0), before.MowerPosition);
			Assert.AreEqual(tileBefore, before.GetTile(1));
			Assert.AreEqual(TileSpec.Grass(3), before.GetTile(1));
			Assert.AreEqual(Create("grass:1 grass:3 grass:2").Snapshot(), before);
		}

		[Test]
		public void Snapshot_DistinguishesPositionAndStatus()
		{
			LevelSimulation a = Create("wood wood grass:2");
			LevelSimulation b = Create("wood wood grass:2", 1, 0);
			Assert.AreNotEqual(a.Snapshot(), b.Snapshot());

			Assert.AreNotEqual(Create("grass:2 grass:0").Snapshot(), Create("grass:2 grass:1").Snapshot());
		}

		[Test]
		public void Snapshot_HashSetDeduplicatesTranspositions()
		{
			LevelSimulation viaRight = Create("wood wood / wood grass:3");
			LevelSimulation viaUp = Create("wood wood / wood grass:3");
			Play(viaRight, "RU");
			Play(viaUp, "UR");

			HashSet<SimulationSnapshot> seen = new HashSet<SimulationSnapshot> { viaRight.Snapshot() };

			Assert.IsFalse(seen.Add(viaUp.Snapshot()));
			Assert.AreEqual(1, seen.Count);
		}

		[Test]
		public void Clone_HasAnEqualSnapshotAndStatus_IncludingFailed()
		{
			LevelSimulation sim = Create("grass:2 grass:1 grass:2");
			sim.Move(R);
			Assert.AreEqual(LevelStatus.Failed, sim.Status);

			LevelSimulation clone = sim.Clone(new UndoSystem());

			Assert.AreEqual(sim.Snapshot(), clone.Snapshot());
			Assert.AreEqual(LevelStatus.Failed, clone.Status);
			Assert.AreEqual(sim.MowerPosition.Value, clone.MowerPosition.Value);
		}

		[Test]
		public void Clone_IsIndependent_BothWays()
		{
			LevelSimulation sim = Create(Mixed);
			sim.Move(R);
			LevelSimulation clone = sim.Clone(new UndoSystem());
			SimulationSnapshot shared = sim.Snapshot();

			clone.Move(U);
			Assert.AreEqual(shared, sim.Snapshot());

			sim.Move(L);
			Assert.AreNotEqual(sim.Snapshot(), clone.Snapshot());
			Assert.AreNotSame(sim.Level, clone.Level);
		}

		[Test]
		public void Clone_HasEmptyHistory_AndNoSubscribers()
		{
			LevelSimulation sim = Create(Mixed, 0, 0, out UndoSystem undo);
			int originalEvents = 0;
			sim.Moved += (r, inv) => originalEvents++;
			sim.Level.GetTile(1, 0).TraversedOnto += (d, inv) => originalEvents++;
			sim.Move(R);
			originalEvents = 0;

			UndoSystem cloneUndo = new UndoSystem();
			LevelSimulation clone = sim.Clone(cloneUndo);
			Assert.IsFalse(cloneUndo.Undo());

			clone.Move(L);
			clone.Move(R);
			Assert.AreEqual(0, originalEvents);
		}

		[Test]
		public void Clone_OfACompletedSimulation_RejectsMoves()
		{
			LevelSimulation sim = Create("grass:1 grass:2");
			sim.Move(R);

			LevelSimulation clone = sim.Clone(new UndoSystem());

			Assert.AreEqual(LevelStatus.Completed, clone.Status);
			Assert.AreEqual(MoveOutcome.Rejected, clone.Move(L).Outcome);
		}

		/// <summary>
		/// 3x3 grass:2 with a grass:1 start: every other tile needs exactly one visit and any
		/// revisit ruins a tile, so the shortest solution is 8 moves.
		/// </summary>
		[Test]
		public void BreadthFirstSearchSmoke_FindsTheKnownShortestSolution_Deterministically()
		{
			LevelDefinition def = LevelText.Parse("grass:1 grass:2 grass:2 / grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2", V(0, 0));

			(int length, int visited) first = Bfs(def);
			(int length, int visited) second = Bfs(def);

			Assert.AreEqual(8, first.length);
			Assert.AreEqual(first, second);
		}

		private static (int length, int visited) Bfs(LevelDefinition def)
		{
			LevelSimulation root = Create(def, new UndoSystem());
			HashSet<SimulationSnapshot> seen = new HashSet<SimulationSnapshot> { root.Snapshot() };
			Queue<(LevelSimulation sim, int depth)> queue = new Queue<(LevelSimulation, int)>();
			queue.Enqueue((root, 0));

			while (queue.Count > 0)
			{
				(LevelSimulation sim, int depth) = queue.Dequeue();
				foreach (GridVector d in Directions)
				{
					LevelSimulation next = sim.Clone(new UndoSystem());
					if (next.Move(d).Outcome != MoveOutcome.Moved || next.Status == LevelStatus.Failed || !seen.Add(next.Snapshot()))
					{
						continue;
					}

					if (next.Status == LevelStatus.Completed)
					{
						return (depth + 1, seen.Count);
					}

					queue.Enqueue((next, depth + 1));
				}
			}

			return (-1, seen.Count);
		}
	}
}
