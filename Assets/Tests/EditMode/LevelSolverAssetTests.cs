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
		/// <remarks>Empty since S2 retired Level6 (13x9) and Level8 (14x9) to Data/Archive.</remarks>
		private static readonly Dictionary<string, string> Capped = new Dictionary<string, string>();

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
			string[] listed = ListedLevels().Select(p => System.IO.Path.GetFileNameWithoutExtension(p)).ToArray();
			CollectionAssert.IsSubsetOf(Capped.Keys, listed);
		}

		/// <summary>
		/// The Bunny Heaven difficulty curve (Assets/Docs/LEVELS.md): levels 1-3 are grass only and at most
		/// 4x4, levels 4-6 add stone, wood and tall grass but no springs or water, every level fits 6x6, and
		/// par never drops except on a level that introduces a tile kind.
		/// </summary>
		[Test]
		public void LevelList_FollowsTheDifficultyCurve()
		{
			LevelData[] levels = ListedLevels().Select(AssetDatabase.LoadAssetAtPath<LevelData>).ToArray();
			Assert.AreEqual(10, levels.Length);

			HashSet<TileKind> seen = new HashSet<TileKind>();
			int previousPar = 0;
			for (int i = 0; i < levels.Length; i++)
			{
				LevelDefinition def = levels[i].ToDefinition();
				string label = $"level {i + 1} ({levels[i].name})";
				HashSet<TileKind> kinds = new HashSet<TileKind>(def.Tiles.Select(t => t.Kind));

				Assert.That(def.Width, Is.LessThanOrEqualTo(6), label);
				Assert.That(def.Depth, Is.LessThanOrEqualTo(6), label);
				Assert.Greater(levels[i].Par, 0, label);

				if (i < 3)
				{
					Assert.That(def.Width * def.Depth, Is.LessThanOrEqualTo(16), label);
					CollectionAssert.IsSubsetOf(kinds, new[] { TileKind.Grass, TileKind.Empty }, label);
					Assert.That(def.Tiles.Where(t => t.Kind == TileKind.Grass).Select(t => t.GrassHeight), Has.All.LessThanOrEqualTo(2), label);
				}
				else if (i < 6)
				{
					CollectionAssert.DoesNotContain(kinds, TileKind.Spring, label);
					CollectionAssert.DoesNotContain(kinds, TileKind.Water, label);
				}

				bool introducesAKind = kinds.Any(k => k != TileKind.Empty && !seen.Contains(k));
				if (!introducesAKind)
				{
					Assert.That(levels[i].Par, Is.GreaterThanOrEqualTo(previousPar), $"{label}: par drops without a new mechanic");
				}

				seen.UnionWith(kinds);
				previousPar = levels[i].Par;
			}

			CollectionAssert.IsSubsetOf(new[] { TileKind.Stone, TileKind.Wood, TileKind.Water, TileKind.Spring }, seen);
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
