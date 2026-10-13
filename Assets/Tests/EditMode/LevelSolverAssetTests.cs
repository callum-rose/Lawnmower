using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Game.Levels;
using Lawnmower.Model;
using Lawnmower.Solver;
using NUnit.Framework;
using UnityEditor;

namespace Tests.EditMode
{
	/// <summary>Every level in the level list, judged by <see cref="LevelSolver"/>.</summary>
	public class LevelSolverAssetTests
	{
		private const string ManagerPath = "Assets/ScriptableObjects/Game/Levels/Functionals/LevelDataManager.asset";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

		/// <summary>Levels the solver can't finish within the default node cap. They keep par 0 ("unknown").</summary>
		private static readonly Dictionary<string, string> Capped = new Dictionary<string, string>
		{
			["Level6"] = "13x9: BFS stores over 1,000,000 states before reaching a shortest solution, so par is unknown " +
				"(its recorded DFS solution still replays in LevelAssetSimulationTests; S2 retires it)",
			["Level8"] = "14x9: BFS stores over 1,000,000 states before reaching a shortest solution, so par is unknown " +
				"(its recorded DFS solution still replays in LevelAssetSimulationTests; S2 retires it)",
		};

		private static IEnumerable<string> ListedLevels()
		{
			SerializedProperty list = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(ManagerPath)).FindProperty("levelDatas");
			for (int i = 0; i < list.arraySize; i++)
			{
				yield return AssetDatabase.GetAssetPath(list.GetArrayElementAtIndex(i).objectReferenceValue);
			}
		}

		[TestCaseSource(nameof(ListedLevels))]
		public void ListedLevel_IsSolvable_WithRecordedPar(string path)
		{
			LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
			Assert.IsNotNull(asset, path);

			if (Capped.TryGetValue(asset.name, out string reason))
			{
				Assert.AreEqual(0, asset.Par, "a capped level has no par");
				Assert.Ignore($"Too big for the solver: {reason}");
			}

			SolveResult result = SolveOffMainThread(asset.ToDefinition());

			Assert.AreEqual(SolveStatus.Solved, result.Status, result.ToString());
			Assert.AreEqual(result.Par, asset.Par, "par is stale: run Bunny Heaven/Recompute Par");
			LevelAssetSimulationTests.AssertSolves(asset, result.SolutionText);

			Assert.IsTrue(MoveNotation.TryParse(asset.Solution, out IReadOnlyList<Game.Core.GridVector> recorded));
			Assert.AreEqual(asset.Par, recorded.Count, "the recorded solution is a shortest one");
		}

		[Test]
		public void CappedLevels_AreStillInTheList()
		{
			// Drop an entry from Capped when S2 retires that level.
			string[] listed = ListedLevels().Select(p => System.IO.Path.GetFileNameWithoutExtension(p)).ToArray();
			CollectionAssert.IsSubsetOf(Capped.Keys, listed);
		}

		/// <summary>Par only (stops at the first, shortest, solution), on a worker thread with a timeout.</summary>
		private static SolveResult SolveOffMainThread(LevelDefinition definition)
		{
			using CancellationTokenSource cancel = new CancellationTokenSource(Timeout);
			Task<SolveResult> task = Task.Run(() => LevelSolver.Solve(definition, new SolverOptions { ExploreAll = false }, cancel.Token));
			Assert.IsTrue(task.Wait(Timeout + TimeSpan.FromSeconds(10)), "solver overran its own cancellation");
			return task.Result;
		}
	}
}
