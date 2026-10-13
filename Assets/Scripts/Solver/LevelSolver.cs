using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Game.Core;
using Lawnmower.Model;

namespace Lawnmower.Solver
{
	/// <summary>
	/// Breadth-first search over <see cref="LevelSimulation"/>, so the solver plays by exactly the
	/// game's rules. Each stored state is a compact key derived from <see cref="LevelSimulation.Snapshot"/>;
	/// expanding a state rebuilds its simulation and plays each direction on a
	/// <see cref="LevelSimulation.Clone"/>. Failed states are dead ends (grass never grows back),
	/// Completed states are goals.
	/// <para>
	/// Pure and synchronous: callers on the Unity editor's main thread must run it on a worker thread
	/// (or inside a test / a menu item with a cancelable progress bar), never inline.
	/// </para>
	/// </summary>
	public static class LevelSolver
	{
		private static readonly GridVector[] Directions = { GridVector.Up, GridVector.Right, GridVector.Down, GridVector.Left };

		// Edge codes; >= 0 is the id of a stored Playing state.
		private const int NoMove = -1;
		private const int ToFailed = -2;
		private const int ToCompleted = -3;

		public static SolveResult Solve(LevelDefinition definition, SolverOptions options = null,
			CancellationToken cancellation = default)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			return new Search(definition, options ?? new SolverOptions(), cancellation).Run();
		}

		/// <summary>Plays <paramref name="moves"/> on a fresh simulation; true if every move moved and the last one completed the level.</summary>
		public static bool Verify(LevelDefinition definition, IReadOnlyList<GridVector> moves)
		{
			LevelSimulation sim = new LevelSimulation(Level.FromDefinition(definition), NoHistoryUndoSystem.Instance);
			foreach (GridVector move in moves)
			{
				if (sim.Status != LevelStatus.Playing || sim.Move(move).Outcome != MoveOutcome.Moved)
				{
					return false;
				}
			}

			return sim.Status == LevelStatus.Completed;
		}

		private sealed class Search
		{
			private readonly LevelDefinition _definition;
			private readonly SolverOptions _options;
			private readonly CancellationToken _cancellation;
			private readonly Stopwatch _watch = Stopwatch.StartNew();

			// Every distinct TileSpec seen, so a tile's state fits in one byte of the key.
			private readonly Dictionary<TileSpec, byte> _paletteIndex = new Dictionary<TileSpec, byte>();
			private readonly List<TileSpec> _palette = new List<TileSpec>();

			private readonly Dictionary<StateKey, int> _ids = new Dictionary<StateKey, int>();
			private readonly List<StateKey> _keys = new List<StateKey>();
			private readonly List<int> _parents = new List<int>();
			private readonly List<byte> _moves = new List<byte>();
			private readonly List<int> _edges = new List<int>(); // 4 per state, in Directions order

			public Search(LevelDefinition definition, SolverOptions options, CancellationToken cancellation)
			{
				_definition = definition;
				_options = options;
				_cancellation = cancellation;
			}

			public SolveResult Run()
			{
				LevelSimulation start = new LevelSimulation(Level.FromDefinition(_definition), NoHistoryUndoSystem.Instance);
				switch (start.Status)
				{
					case LevelStatus.Completed:
						return Result(SolveStatus.Solved, new GridVector[0], true, null);
					case LevelStatus.Failed:
						return Result(SolveStatus.Unsolvable, null, true, "the level starts failed");
				}

				Add(Encode(start), -1, 0);

				List<GridVector> solution = null;
				for (int head = 0; head < _keys.Count; head++)
				{
					if ((head & 0xFFF) == 0)
					{
						if (_cancellation.IsCancellationRequested)
						{
							return Stopped(solution, SolveStatus.Cancelled, "cancelled");
						}

						_options.Progress?.Invoke(_keys.Count);
					}

					LevelSimulation sim = head == 0 ? start : Decode(_keys[head]);
					for (byte d = 0; d < Directions.Length; d++)
					{
						LevelSimulation child = sim.Clone(NoHistoryUndoSystem.Instance);
						int edge;

						if (child.Move(Directions[d]).Outcome != MoveOutcome.Moved)
						{
							edge = NoMove;
						}
						else if (child.Status == LevelStatus.Completed)
						{
							edge = ToCompleted;
							if (solution == null)
							{
								solution = PathTo(head);
								solution.Add(Directions[d]);
								if (!_options.ExploreAll)
								{
									return Result(SolveStatus.Solved, solution, false, null);
								}
							}
						}
						else if (child.Status == LevelStatus.Failed)
						{
							edge = ToFailed;
						}
						else
						{
							StateKey key = Encode(child);
							if (!_ids.TryGetValue(key, out edge))
							{
								if (_keys.Count >= _options.NodeCap)
								{
									return Stopped(solution, SolveStatus.TooBig,
										$"node cap of {_options.NodeCap:N0} states reached");
								}

								edge = Add(key, head, d);
							}
						}

						_edges.Add(edge);
					}
				}

				if (solution == null)
				{
					return Result(SolveStatus.Unsolvable, null, true, null);
				}

				CountDeadEnds(out int firstMoves, out int deadEnds);
				return new SolveResult(SolveStatus.Solved, solution, true, _keys.Count, firstMoves, deadEnds, _watch.Elapsed, null);
			}

			/// <summary>A BFS solution found before stopping is still a shortest one.</summary>
			private SolveResult Stopped(List<GridVector> solution, SolveStatus status, string reason) =>
				solution != null
					? Result(SolveStatus.Solved, solution, false, $"{reason} after the solution was found; statistics are partial")
					: Result(status, null, false, reason);

			private SolveResult Result(SolveStatus status, IReadOnlyList<GridVector> solution, bool exhaustive, string reason) =>
				new SolveResult(status, solution, exhaustive, _keys.Count, 0, 0, _watch.Elapsed, reason);

			private int Add(StateKey key, int parent, byte move)
			{
				int id = _keys.Count;
				_ids.Add(key, id);
				_keys.Add(key);
				_parents.Add(parent);
				_moves.Add(move);
				return id;
			}

			private List<GridVector> PathTo(int id)
			{
				List<GridVector> path = new List<GridVector>();
				for (; _parents[id] >= 0; id = _parents[id])
				{
					path.Add(Directions[_moves[id]]);
				}

				path.Reverse();
				return path;
			}

			/// <summary>Marks every state that can still reach a win (backwards from the winning moves), then checks the start's moves.</summary>
			private void CountDeadEnds(out int firstMoves, out int deadEnds)
			{
				int count = _keys.Count;
				bool[] solvable = new bool[count];
				int[] reverseStart = new int[count + 1];
				for (int i = 0; i < _edges.Count; i++)
				{
					if (_edges[i] >= 0)
					{
						reverseStart[_edges[i] + 1]++;
					}
				}

				for (int i = 0; i < count; i++)
				{
					reverseStart[i + 1] += reverseStart[i];
				}

				int[] fill = (int[])reverseStart.Clone();
				int[] reverse = new int[reverseStart[count]];
				Queue<int> queue = new Queue<int>();
				for (int i = 0; i < _edges.Count; i++)
				{
					int from = i / Directions.Length;
					if (_edges[i] >= 0)
					{
						reverse[fill[_edges[i]]++] = from;
					}
					else if (_edges[i] == ToCompleted && !solvable[from])
					{
						solvable[from] = true;
						queue.Enqueue(from);
					}
				}

				while (queue.Count > 0)
				{
					int state = queue.Dequeue();
					for (int i = reverseStart[state]; i < reverseStart[state + 1]; i++)
					{
						if (!solvable[reverse[i]])
						{
							solvable[reverse[i]] = true;
							queue.Enqueue(reverse[i]);
						}
					}
				}

				firstMoves = 0;
				deadEnds = 0;
				for (int d = 0; d < Directions.Length; d++)
				{
					int edge = _edges[d];
					if (edge == NoMove)
					{
						continue;
					}

					firstMoves++;
					if (edge == ToFailed || edge >= 0 && !solvable[edge])
					{
						deadEnds++;
					}
				}
			}

			private StateKey Encode(LevelSimulation sim)
			{
				SimulationSnapshot snapshot = sim.Snapshot();
				byte[] bytes = new byte[2 + snapshot.TileCount];
				int position = snapshot.MowerPosition.x + snapshot.MowerPosition.y * _definition.Width;
				bytes[0] = (byte)position;
				bytes[1] = (byte)(position >> 8);

				for (int i = 0; i < snapshot.TileCount; i++)
				{
					TileSpec tile = snapshot.GetTile(i);
					if (!_paletteIndex.TryGetValue(tile, out byte index))
					{
						if (_palette.Count > byte.MaxValue)
						{
							throw new InvalidOperationException("More than 256 distinct tile states in one level");
						}

						index = (byte)_palette.Count;
						_paletteIndex.Add(tile, index);
						_palette.Add(tile);
					}

					bytes[2 + i] = index;
				}

				return new StateKey(bytes);
			}

			/// <summary>A tile's spec is its whole state (Tile.Clone round-trips through it), so this rebuilds the state exactly.</summary>
			private LevelSimulation Decode(StateKey key)
			{
				byte[] bytes = key.Bytes;
				int position = bytes[0] | bytes[1] << 8;
				TileSpec[] tiles = new TileSpec[bytes.Length - 2];
				for (int i = 0; i < tiles.Length; i++)
				{
					tiles[i] = _palette[bytes[2 + i]];
				}

				GridVector mower = new GridVector(position % _definition.Width, position / _definition.Width);
				LevelDefinition state = new LevelDefinition(_definition.Id, _definition.Width, _definition.Depth, mower, tiles);
				return new LevelSimulation(Level.FromDefinition(state), NoHistoryUndoSystem.Instance);
			}
		}

		private sealed class StateKey : IEquatable<StateKey>
		{
			public readonly byte[] Bytes;
			private readonly int _hash;

			public StateKey(byte[] bytes)
			{
				Bytes = bytes;
				unchecked
				{
					uint hash = 2166136261;
					foreach (byte b in bytes)
					{
						hash = (hash ^ b) * 16777619;
					}

					_hash = (int)hash;
				}
			}

			public bool Equals(StateKey other) =>
				other != null && _hash == other._hash && Bytes.AsSpan().SequenceEqual(other.Bytes);

			public override bool Equals(object obj) => obj is StateKey other && Equals(other);

			public override int GetHashCode() => _hash;
		}
	}
}
