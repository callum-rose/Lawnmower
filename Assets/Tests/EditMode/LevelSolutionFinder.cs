using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Core;
using Game.Levels;
using Game.UndoSystem;
using Lawnmower.Model;
using NUnit.Framework;
using UnityEditor;

namespace Tests.EditMode
{
	/// <summary>
	/// THROWAWAY helper, not part of the gate: S1's solver replaces it and S1 should delete this file.
	/// A bounded depth-first search over <see cref="LevelSimulation"/> (Move/Undo, Failed states pruned,
	/// a visited set of 64-bit state hashes) per shipped level, capped at 2,000,000 nodes and 60 s per
	/// level. Searches run on worker threads, never the editor's main thread. Found solutions are
	/// written to Temp/LevelSolutions.txt; they are not shortest (par is S1's job).
	/// </summary>
	public class LevelSolutionFinder
	{
		private const int NodeCap = 2_000_000;
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

		[Test, Explicit("Throwaway solution finder (S1 replaces it); run by hand")]
		public void FindSolutionsForShippedLevels()
		{
			List<(string name, LevelDefinition def)> levels = AssetDatabase
				.FindAssets("t:LevelData", new[] { "Assets/ScriptableObjects/Game/Levels/Data" })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(p => (Path.GetFileNameWithoutExtension(p), AssetDatabase.LoadAssetAtPath<LevelData>(p).ToDefinition()))
				.OrderBy(l => l.Item1, StringComparer.Ordinal)
				.ToList();

			Task<string>[] searches = levels
				.Select(l => Task.Run(() => $"{l.name}\t{Search(l.def)}"))
				.ToArray();

			Assert.IsTrue(Task.WaitAll(searches, Timeout + TimeSpan.FromSeconds(30)), "searches overran their own timeout");

			string report = string.Join("\n", searches.Select(t => t.Result));
			File.WriteAllText("Temp/LevelSolutions.txt", report + "\n");
			TestContext.WriteLine(report);
		}

		/// <summary>"solution\tnodes\tms", or "-\treason\tnodes\tms" when none was found within budget.</summary>
		private static string Search(LevelDefinition def)
		{
			Stopwatch watch = Stopwatch.StartNew();
			using CancellationTokenSource cancel = new CancellationTokenSource(Timeout);
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Level.FromDefinition(def), undo);
			HashSet<ulong> visited = new HashSet<ulong> { Key(sim) };
			List<GridVector> path = new List<GridVector>();
			int nodes = 0;
			string outcome = "exhausted";

			bool Dfs()
			{
				foreach (GridVector d in new[] { GridVector.Up, GridVector.Right, GridVector.Down, GridVector.Left })
				{
					if (++nodes > NodeCap)
					{
						outcome = "node cap";
						return false;
					}

					if (cancel.IsCancellationRequested)
					{
						outcome = "timeout";
						return false;
					}

					if (sim.Move(d).Outcome != MoveOutcome.Moved)
					{
						continue;
					}

					path.Add(d);
					if (sim.Status == LevelStatus.Completed)
					{
						return true;
					}

					if (sim.Status == LevelStatus.Playing && visited.Add(Key(sim)) && Dfs())
					{
						return true;
					}

					if (outcome != "exhausted")
					{
						return false;
					}

					path.RemoveAt(path.Count - 1);
					undo.Undo();
				}

				return false;
			}

			bool found = sim.Status == LevelStatus.Playing && Dfs();
			return found
				? $"{MoveNotation.Format(path)}\t{nodes}\t{watch.ElapsedMilliseconds}"
				: $"-\t{outcome}\t{nodes}\t{watch.ElapsedMilliseconds}";
		}

		/// <summary>FNV-1a over position and tile state. A collision only hides a branch; found solutions are verified by replay.</summary>
		private static ulong Key(LevelSimulation sim)
		{
			const ulong prime = 1099511628211;
			ulong hash = 14695981039346656037;

			void Mix(int value)
			{
				hash ^= (uint)value;
				hash *= prime;
			}

			GridVector p = sim.MowerPosition.Value;
			Mix(p.x);
			Mix(p.y);
			foreach (var tile in sim.Level.Tiles)
			{
				TileSpec spec = tile.ToSpec();
				Mix((int)spec.Kind * 16 + spec.GrassHeight);
			}

			return hash;
		}
	}
}
