using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Game.Core;
using Game.UndoSystem;
using Lawnmower.Model;
using Lawnmower.Model.Tests;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Solver.Tests
{
	public class LevelSolverTests
	{
		/// <summary>3x3 grass:2 with a grass:1 start: every other tile needs exactly one visit.</summary>
		private const string Snake3x3 = "grass:1 grass:2 grass:2 / grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2";

		/// <summary>
		/// Start (1,0). L ruins (0,0) at once; U-R-D and R-U-L both win. Playing states: start, U, R, UR, RU.
		/// </summary>
		private const string Fork = "grass:1 grass:1 grass:2 / stone:0 grass:2 grass:2";

		private static SolveResult Solve(string rows, int x = 0, int y = 0, SolverOptions options = null) =>
			LevelSolver.Solve(LevelText.Parse(rows, V(x, y)), options);

		/// <summary>The solution replays through a normal simulation (with real undo) and wins on its last move.</summary>
		private static void AssertReplays(LevelDefinition def, SolveResult result)
		{
			LevelSimulation sim = Create(def, new UndoSystem());
			for (int i = 0; i < result.Solution.Count; i++)
			{
				Assert.AreEqual(LevelStatus.Playing, sim.Status, $"before move {i + 1}");
				Assert.AreEqual(MoveOutcome.Moved, sim.Move(result.Solution[i]).Outcome, $"move {i + 1}");
			}

			Assert.AreEqual(LevelStatus.Completed, sim.Status);
			Assert.IsTrue(LevelSolver.Verify(def, result.Solution));
		}

		[Test]
		public void Corridor_ParTwo()
		{
			SolveResult result = Solve("grass:1 grass:2 grass:2");

			Assert.AreEqual(SolveStatus.Solved, result.Status);
			Assert.AreEqual(2, result.Par);
			Assert.AreEqual("RR", result.SolutionText);
			Assert.IsTrue(result.Exhaustive);
			Assert.AreEqual(2, result.ReachableStates);
			Assert.AreEqual(1, result.FirstMoves);
			Assert.AreEqual(0, result.DeadEndFirstMoves);
			Assert.AreEqual(0, result.DeadEndShare);
			Assert.AreEqual(SolveResult.ComputeDifficulty(2, 2, 0), result.Difficulty);
		}

		[Test]
		public void Snake3x3_ParEight()
		{
			LevelDefinition def = LevelText.Parse(Snake3x3, V(0, 0));
			SolveResult result = LevelSolver.Solve(def);

			Assert.AreEqual(SolveStatus.Solved, result.Status);
			Assert.AreEqual(8, result.Par);
			AssertReplays(def, result);
		}

		[Test]
		public void Fork_CountsStatesAndDeadEndFirstMoves()
		{
			LevelDefinition def = LevelText.Parse(Fork, V(1, 0));
			SolveResult result = LevelSolver.Solve(def);

			Assert.AreEqual(SolveStatus.Solved, result.Status);
			Assert.AreEqual(3, result.Par);
			Assert.AreEqual("URD", result.SolutionText, "BFS tries U, R, D, L in that order");
			Assert.AreEqual(5, result.ReachableStates);
			Assert.AreEqual(3, result.FirstMoves, "L, U and R move; D is off the board");
			Assert.AreEqual(1, result.DeadEndFirstMoves, "L ruins (0,0)");
			Assert.AreEqual(1.0 / 3, result.DeadEndShare, 1e-9);
			Assert.AreEqual(4.7, result.Difficulty, 1e-9, "3 x (1 + 1/3) x log10(15)");
			AssertReplays(def, result);
		}

		[Test]
		public void Wood_IsCrossedWithoutMowing()
		{
			// Start on the wood: mow one end, cross back over the wood, mow the other.
			LevelDefinition def = LevelText.Parse("grass:2 wood grass:2", V(1, 0));
			SolveResult result = LevelSolver.Solve(def);

			Assert.AreEqual(3, result.Par);
			Assert.AreEqual(2, result.FirstMoves);
			Assert.AreEqual(0, result.DeadEndFirstMoves, "either end can be mowed first");
			AssertReplays(def, result);
		}

		[Test]
		public void Spring_IsPartOfTheRules()
		{
			LevelDefinition def = LevelText.Parse("grass:1 spring:0,1 / grass:2 grass:2", V(0, 0));
			SolveResult result = LevelSolver.Solve(def);

			Assert.AreEqual(2, result.Par);
			AssertReplays(def, result);
		}

		[Test]
		public void AlreadyComplete_ParZero()
		{
			SolveResult result = Solve("grass:1 wood");

			Assert.AreEqual(SolveStatus.Solved, result.Status);
			Assert.AreEqual(0, result.Par);
			Assert.AreEqual("", result.SolutionText);
		}

		[Test]
		public void Walled_IsUnsolvable()
		{
			SolveResult result = Solve("grass:2 stone:0 grass:2");

			Assert.AreEqual(SolveStatus.Unsolvable, result.Status);
			Assert.IsNull(result.Solution);
			Assert.AreEqual(-1, result.Par);
			Assert.IsTrue(result.Exhaustive);
			Assert.AreEqual(1, result.ReachableStates);
			Assert.IsNaN(result.Difficulty);
		}

		[Test]
		public void StartsFailed_IsUnsolvable()
		{
			Assert.AreEqual(SolveStatus.Unsolvable, Solve("grass:2 grass:0").Status);
		}

		[Test]
		public void NodeCap_ReportsTooBig_NotUnsolvable()
		{
			string big = string.Join(" / ", Enumerable.Repeat("grass:3 grass:3 grass:3 grass:3 grass:3", 5));
			SolveResult result = Solve(big, options: new SolverOptions { NodeCap = 50 });

			Assert.AreEqual(SolveStatus.TooBig, result.Status);
			Assert.IsNull(result.Solution);
			Assert.IsFalse(result.Exhaustive);
			Assert.AreEqual(50, result.ReachableStates);
			StringAssert.Contains("cap", result.Reason);
		}

		[Test]
		public void Cancellation_StopsTheSearch()
		{
			using CancellationTokenSource cancel = new CancellationTokenSource();
			cancel.Cancel();

			SolveResult result = LevelSolver.Solve(LevelText.Parse(Snake3x3, V(0, 0)), null, cancel.Token);

			Assert.AreEqual(SolveStatus.Cancelled, result.Status);
		}

		[Test]
		public void FirstSolutionOnly_GivesTheSamePar_WithoutStatistics()
		{
			SolveResult quick = Solve(Snake3x3, options: new SolverOptions { ExploreAll = false });

			Assert.AreEqual(SolveStatus.Solved, quick.Status);
			Assert.AreEqual(Solve(Snake3x3).Par, quick.Par);
			Assert.IsFalse(quick.Exhaustive);
			Assert.IsNaN(quick.DeadEndShare);
		}

		[Test]
		public void Progress_IsReported()
		{
			List<int> reports = new List<int>();
			Solve(Snake3x3, options: new SolverOptions { Progress = reports.Add });

			Assert.IsNotEmpty(reports);
		}

		[Test]
		public void IsDeterministic()
		{
			SolveResult a = Solve(Snake3x3);
			SolveResult b = Solve(Snake3x3);

			Assert.AreEqual(a.SolutionText, b.SolutionText);
			Assert.AreEqual(a.ReachableStates, b.ReachableStates);
			Assert.AreEqual(a.DeadEndFirstMoves, b.DeadEndFirstMoves);
		}

		[Test]
		public void Difficulty_GrowsWithEachInput()
		{
			double baseline = SolveResult.ComputeDifficulty(10, 100, 0);

			Assert.Greater(SolveResult.ComputeDifficulty(11, 100, 0), baseline);
			Assert.Greater(SolveResult.ComputeDifficulty(10, 1000, 0), baseline);
			Assert.Greater(SolveResult.ComputeDifficulty(10, 100, 0.5), baseline);
		}

		[TestCaseSource(nameof(Seeds))]
		public void RandomLevels_SolutionsReplay_AndUnsolvableMeansExhausted(int seed)
		{
			LevelDefinition def = RandomLevels.Generate(seed);
			SolveResult result = LevelSolver.Solve(def, new SolverOptions { NodeCap = 200_000 });

			switch (result.Status)
			{
				case SolveStatus.Solved:
					if (result.Par > 0)
					{
						AssertReplays(def, result);
					}

					break;
				case SolveStatus.Unsolvable:
					Assert.IsTrue(result.Exhaustive);
					break;
				default:
					Assert.Fail(result.ToString());
					break;
			}
		}

		private static IEnumerable<int> Seeds() => Enumerable.Range(1, 30);

		[Test]
		public void NoHistoryUndoSystem_RunsAndForgets()
		{
			LevelSimulation sim = Create(LevelText.Parse("grass:1 grass:2 grass:2", V(0, 0)), NoHistoryUndoSystem.Instance);

			Assert.AreEqual(MoveOutcome.Moved, sim.Move(R).Outcome);
			Assert.AreEqual(V(1, 0), sim.MowerPosition.Value);
			Assert.IsFalse(sim.UndoSystem.Undo());
			Assert.AreEqual(0, sim.UndoSystem.Count);
		}

		[Test]
		public void SolverAssembly_ReferencesOnlyTheModel()
		{
			string[] references = typeof(LevelSolver).Assembly.GetReferencedAssemblies().Select(a => a.Name).ToArray();

			CollectionAssert.Contains(references, "Lawnmower.Model");
			Assert.That(references.Where(r => r.StartsWith("Unity") || r == "Scripts" || r.StartsWith("Assembly-CSharp")),
				Is.Empty, string.Join(", ", references));
		}
	}
}
