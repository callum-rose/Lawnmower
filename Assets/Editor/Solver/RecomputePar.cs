using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Core;
using Game.Levels;
using Lawnmower.Model;
using Lawnmower.Solver;
using UnityEditor;
using UnityEngine;

namespace Lawnmower.Editor.Solver
{
	/// <summary>
	/// Solves every level asset on worker threads (the main thread only shows a cancelable progress
	/// bar) and writes each solved level's par, plus a shortest solution when the recorded one is
	/// missing, wrong or longer. Levels that hit the node cap get par 0 ("unknown"). A Markdown
	/// report is logged and written to Temp/ParReport.md.
	/// </summary>
	public static class RecomputePar
	{
		public const string LevelsDir = "Assets/ScriptableObjects/Game/Levels";
		public const string ManagerPath = LevelsDir + "/Functionals/LevelDataManager.asset";
		public const string ReportPath = "Temp/ParReport.md";

		[MenuItem("Bunny Heaven/Recompute Par")]
		public static void RecomputeMenu() => Run();

		/// <summary>Also callable from CLI eval. Returns the Markdown report.</summary>
		public static string Run(int nodeCap = SolverOptions.DefaultNodeCap)
		{
			List<(string path, LevelData asset)> levels = AssetDatabase.FindAssets("t:LevelData", new[] { LevelsDir })
				.Select(AssetDatabase.GUIDToAssetPath)
				.OrderBy(p => p, StringComparer.Ordinal)
				.Select(p => (p, AssetDatabase.LoadAssetAtPath<LevelData>(p)))
				.ToList();

			LevelDefinition[] definitions = levels.Select(l => l.asset.ToDefinition()).ToArray();
			SolveResult[] results = new SolveResult[levels.Count];
			int[] states = new int[levels.Count];
			int done = 0;

			using CancellationTokenSource cancel = new CancellationTokenSource();
			Task solving = Task.Run(() => Parallel.For(0, levels.Count,
				new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Math.Min(4, Environment.ProcessorCount - 1)) },
				i =>
				{
					results[i] = LevelSolver.Solve(definitions[i],
						new SolverOptions { NodeCap = nodeCap, Progress = n => Volatile.Write(ref states[i], n) },
						cancel.Token);
					Interlocked.Increment(ref done);
				}));

			try
			{
				while (!solving.Wait(100))
				{
					int finished = Volatile.Read(ref done);
					long total = states.Sum(s => (long)s);
					if (EditorUtility.DisplayCancelableProgressBar("Recompute Par",
						    $"Solved {finished}/{levels.Count} levels ({total:N0} states stored)",
						    (float)finished / levels.Count))
					{
						cancel.Cancel();
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			HashSet<string> listed = ListedLevelPaths();
			StringBuilder report = new StringBuilder();
			report.AppendLine("| Level | In list | Size | Status | Par | States | Dead-end first moves | Difficulty | Recorded solution |");
			report.AppendLine("|---|---|---|---|---|---|---|---|---|");

			for (int i = 0; i < levels.Count; i++)
			{
				(string path, LevelData asset) = levels[i];
				SolveResult result = results[i];
				Write(asset, definitions[i], result);

				report.AppendLine($"| {asset.name} | {(listed.Contains(path) ? "yes" : "no")} | {definitions[i].Width}×{definitions[i].Depth} | " +
				                  $"{Describe(result)} | {(result.Par >= 0 ? result.Par.ToString() : "?")} | " +
				                  $"{result.ReachableStates:N0}{(result.Exhaustive ? "" : "+")} | " +
				                  $"{(result.Exhaustive && result.Status == SolveStatus.Solved ? $"{result.DeadEndFirstMoves}/{result.FirstMoves}" : "-")} | " +
				                  $"{(double.IsNaN(result.Difficulty) ? "-" : result.Difficulty.ToString("0.0"))} | " +
				                  $"{(result.Solution == null ? "-" : "`" + asset.Solution + "`")} |");
			}

			AssetDatabase.SaveAssets();

			string text = report.ToString();
			Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
			File.WriteAllText(ReportPath, text);
			Debug.Log($"Recompute Par (node cap {nodeCap:N0}):\n{text}");
			return text;
		}

		private static string Describe(SolveResult result) => result.Status switch
		{
			SolveStatus.Solved when !result.Exhaustive => "solved (stats partial)",
			SolveStatus.Solved => "solved",
			SolveStatus.Unsolvable => "**unsolvable**",
			SolveStatus.TooBig => "unknown: too big",
			_ => "cancelled"
		};

		private static void Write(LevelData asset, LevelDefinition definition, SolveResult result)
		{
			SerializedObject so = new SerializedObject(asset);
			int par = result.Status == SolveStatus.Solved ? result.Par : 0;

			if (result.Status == SolveStatus.Cancelled)
			{
				return;
			}

			so.FindProperty("par").intValue = par;

			if (result.Status == SolveStatus.Solved && result.Par > 0)
			{
				SerializedProperty solution = so.FindProperty("solution");
				bool recordedIsShortest = MoveNotation.TryParse(solution.stringValue, out IReadOnlyList<GridVector> moves) &&
				                          moves.Count == result.Par && LevelSolver.Verify(definition, moves);
				if (!recordedIsShortest)
				{
					solution.stringValue = result.SolutionText;
				}
			}

			if (so.ApplyModifiedPropertiesWithoutUndo())
			{
				EditorUtility.SetDirty(asset);
			}
		}

		private static HashSet<string> ListedLevelPaths()
		{
			HashSet<string> paths = new HashSet<string>();
			UnityEngine.Object manager = AssetDatabase.LoadMainAssetAtPath(ManagerPath);
			SerializedProperty list = manager == null ? null : new SerializedObject(manager).FindProperty("levelDatas");
			for (int i = 0; list != null && i < list.arraySize; i++)
			{
				paths.Add(AssetDatabase.GetAssetPath(list.GetArrayElementAtIndex(i).objectReferenceValue));
			}

			return paths;
		}
	}
}
