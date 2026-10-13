using System;
using System.Collections.Generic;
using Game.Core;
using Lawnmower.Model;

namespace Lawnmower.Solver
{
	public enum SolveStatus
	{
		/// <summary>A shortest solution was found.</summary>
		Solved,
		/// <summary>Every reachable state was explored and none completes the level.</summary>
		Unsolvable,
		/// <summary>The node cap was hit before a solution was found: unknown, not a failure.</summary>
		TooBig,
		/// <summary>The caller cancelled before a solution was found.</summary>
		Cancelled
	}

	public sealed class SolverOptions
	{
		public const int DefaultNodeCap = 1_000_000;

		/// <summary>Most distinct states the search may store before it gives up.</summary>
		public int NodeCap { get; set; } = DefaultNodeCap;

		/// <summary>
		/// Keep searching after the first (shortest) solution so <see cref="SolveResult.ReachableStates"/>,
		/// <see cref="SolveResult.DeadEndShare"/> and <see cref="SolveResult.Difficulty"/> are known.
		/// False stops at the first solution (par only).
		/// </summary>
		public bool ExploreAll { get; set; } = true;

		/// <summary>Called from the solving thread about every 4096 expansions with the stored state count.</summary>
		public Action<int> Progress { get; set; }
	}

	public sealed class SolveResult
	{
		public SolveStatus Status { get; }

		/// <summary>A shortest solution, or null if none was found.</summary>
		public IReadOnlyList<GridVector> Solution { get; }

		/// <summary><see cref="Solution"/> in <see cref="MoveNotation"/>, or null.</summary>
		public string SolutionText => Solution == null ? null : MoveNotation.Format(Solution);

		/// <summary>Shortest solution length, or -1 when unknown.</summary>
		public int Par => Solution?.Count ?? -1;

		/// <summary>
		/// True when every reachable state was explored, so the statistics below are exact. When false,
		/// <see cref="ReachableStates"/> is a lower bound and the share and difficulty are NaN.
		/// </summary>
		public bool Exhaustive { get; }

		/// <summary>Distinct Playing states reachable from the start, the start included. Won and failed end states aren't counted.</summary>
		public int ReachableStates { get; }

		/// <summary>First moves that actually move the mower (bumps and off-board moves excluded).</summary>
		public int FirstMoves { get; }

		/// <summary>First moves after which the level can no longer be completed (they fail it, or every continuation does).</summary>
		public int DeadEndFirstMoves { get; }

		/// <summary><see cref="DeadEndFirstMoves"/> / <see cref="FirstMoves"/>; NaN unless exhaustive.</summary>
		public double DeadEndShare { get; }

		/// <summary>
		/// par × (1 + deadEndShare) × log10(10 + reachableStates), rounded to one decimal; NaN unless
		/// the level is solved and the search was exhaustive. Par dominates (it is the minimum work); the
		/// log term grows with how much there is to get lost in (10 states ≈ 1.3, 1,000 ≈ 3, 10⁶ ≈ 6); a
		/// level whose every opening is a trap scores up to twice one with no wrong first move.
		/// </summary>
		public double Difficulty { get; }

		public TimeSpan Elapsed { get; }

		/// <summary>Why the result isn't a plain solve (cap, cancel), or null.</summary>
		public string Reason { get; }

		internal SolveResult(SolveStatus status, IReadOnlyList<GridVector> solution, bool exhaustive, int reachableStates,
			int firstMoves, int deadEndFirstMoves, TimeSpan elapsed, string reason)
		{
			Status = status;
			Solution = solution;
			Exhaustive = exhaustive;
			ReachableStates = reachableStates;
			FirstMoves = firstMoves;
			DeadEndFirstMoves = deadEndFirstMoves;
			Elapsed = elapsed;
			Reason = reason;

			DeadEndShare = exhaustive && firstMoves > 0 ? (double)deadEndFirstMoves / firstMoves :
				exhaustive ? 0 : double.NaN;
			Difficulty = exhaustive && solution != null ? ComputeDifficulty(solution.Count, reachableStates, DeadEndShare) : double.NaN;
		}

		public static double ComputeDifficulty(int par, int reachableStates, double deadEndShare) =>
			Math.Round(par * (1 + deadEndShare) * Math.Log10(10 + reachableStates), 1);

		public override string ToString() =>
			$"{Status} par={Par} states={ReachableStates}{(Exhaustive ? "" : "+")} deadEnds={DeadEndFirstMoves}/{FirstMoves} " +
			$"difficulty={Difficulty} {SolutionText} ({Elapsed.TotalMilliseconds:0} ms){(Reason == null ? "" : " - " + Reason)}";
	}
}
