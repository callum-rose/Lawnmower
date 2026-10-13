using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Core;
using Game.Levels;
using Game.UndoSystem;
using Lawnmower.Model;
using Lawnmower.Model.Tests;
using NUnit.Framework;
using UnityEditor;

namespace Tests.EditMode
{
	/// <summary>Every shipped <see cref="LevelData"/> asset, run through <see cref="LevelSimulation"/>.</summary>
	public class LevelAssetSimulationTests
	{
		private const string DataDir = "Assets/ScriptableObjects/Game/Levels/Data";
		private const string ManagerPath = "Assets/ScriptableObjects/Game/Levels/Functionals/LevelDataManager.asset";
		private const string ArchiveDir = DataDir + "/Archive";
		private const string SpringTest = "Archive/Level Spring Test";

		/// <summary>Every level asset under <see cref="DataDir"/> (retired ones included), as a path relative to it without ".asset".</summary>
		private static IEnumerable<string> LevelNames() =>
			AssetDatabase.FindAssets("t:LevelData", new[] { DataDir })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(p => p.Substring(DataDir.Length + 1, p.Length - DataDir.Length - 1 - ".asset".Length))
				.OrderBy(n => n, StringComparer.Ordinal);

		private static IEnumerable<TestCaseData> LevelsAndSeeds() =>
			from name in LevelNames()
			from seed in Enumerable.Range(1, 10)
			select new TestCaseData(name, seed);

		private static string PathOf(string name) => $"{DataDir}/{name}.asset";

		private static LevelData Load(string name)
		{
			LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(PathOf(name));
			Assert.IsNotNull(asset, name);
			return asset;
		}

		private static string SerializedGuid(LevelData asset) => new SerializedObject(asset).FindProperty("guid").stringValue;

		#region Loading

		[Test]
		public void FindsEveryShippedLevel()
		{
			CollectionAssert.AreEqual(
				new[]
				{
					"Archive/Level Spring Test", "Archive/Level3", "Archive/Level5", "Archive/Level6", "Archive/Level7", "Archive/Level8",
					"Level01", "Level02", "Level03", "Level04", "Level05", "Level06", "Level07", "Level08", "Level09", "Level10"
				},
				LevelNames().ToArray());
		}

		[TestCaseSource(nameof(LevelNames))]
		public void Loads_ToAValidDefinition(string name)
		{
			LevelData asset = Load(name);
			LevelDefinition def = null;
			Assert.DoesNotThrow(() => def = asset.ToDefinition());

			Assert.AreEqual(def.Width * def.Depth, def.Tiles.Count);
			Assert.AreNotEqual(Guid.Empty, def.Id);
			Assert.AreEqual(Guid.Parse(SerializedGuid(asset)), def.Id);
			Assert.IsTrue(def.Contains(def.StartPosition), "start in bounds");
			Assert.That(def[def.StartPosition].Kind, Is.EqualTo(TileKind.Grass).Or.EqualTo(TileKind.Wood), "start tile");
		}

		[TestCaseSource(nameof(LevelNames))]
		public void Id_IsStableAcrossLoadsAndReimport(string name)
		{
			Guid first = Load(name).Id;
			string serialized = SerializedGuid(Load(name));
			Assert.AreEqual(first, Load(name).Id);

			AssetDatabase.ImportAsset(PathOf(name), ImportAssetOptions.ForceUpdate);

			Assert.AreEqual(first, Load(name).Id);
			Assert.AreEqual(serialized, SerializedGuid(Load(name)));
		}

		[Test]
		public void Ids_AreUnique()
		{
			Guid[] ids = LevelNames().Select(n => Load(n).Id).ToArray();
			CollectionAssert.AllItemsAreUnique(ids);
		}

		[Test]
		public void LevelList_IsTheTenCurveLevels_InOrder()
		{
			SerializedProperty list = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(ManagerPath)).FindProperty("levelDatas");
			Assert.IsNotNull(list);

			List<string> paths = new List<string>();
			for (int i = 0; i < list.arraySize; i++)
			{
				UnityEngine.Object entry = list.GetArrayElementAtIndex(i).objectReferenceValue;
				Assert.IsNotNull(entry, $"entry {i} is null");
				paths.Add(AssetDatabase.GetAssetPath(entry));
			}

			// Bunny Heaven ships the ten curve levels (Assets/Docs/LEVELS.md); retired levels stay in Archive/.
			CollectionAssert.AreEqual(Enumerable.Range(1, 10).Select(i => PathOf($"Level{i:00}")).ToArray(), paths);
			Assert.That(paths, Has.None.StartsWith(ArchiveDir + "/"));
		}

		#endregion

		#region Simulation

		[TestCaseSource(nameof(LevelNames))]
		public void Planning_NeverThrows_FromAnyTraversableTile(string name)
		{
			// Also proves shipped data has no spring cycles.
			LevelDefinition def = Load(name).ToDefinition();
			int checkedMoves = 0;

			for (int y = 0; y < def.Depth; y++)
			{
				for (int x = 0; x < def.Width; x++)
				{
					GridVector p = new GridVector(x, y);
					if (!TileFactory.Create(def[p]).IsTraversable)
					{
						continue;
					}

					LevelSimulation sim = new LevelSimulation(Level.FromDefinition(def.WithStartPosition(p)), new UndoSystem());
					Assert.AreNotEqual(LevelStatus.Completed, sim.Status, "a completed level would skip planning");

					foreach (GridVector d in Sim.Directions)
					{
						Assert.DoesNotThrow(() => sim.Preview(d), $"{p} {MoveNotation.ToChar(d)}");
						checkedMoves++;
					}
				}
			}

			Assert.Greater(checkedMoves, 0);
		}

		[TestCaseSource(nameof(LevelNames))]
		public void Play_NeverTouchesTheAsset(string name)
		{
			LevelData asset = Load(name);
			string before = EditorJsonUtility.ToJson(asset);
			LevelDefinition definition = asset.ToDefinition();
			string describedBefore = LevelText.Describe(definition);

			LevelSimulation sim = new LevelSimulation(asset.CreateLevel(), new UndoSystem());
			Random random = new Random(name.Aggregate(17, (h, c) => h * 31 + c));
			for (int i = 0; i < 30; i++)
			{
				sim.Move(Sim.Directions[random.Next(4)]);
			}

			Assert.AreEqual(before, EditorJsonUtility.ToJson(asset));
			Assert.AreEqual(describedBefore, LevelText.Describe(asset.ToDefinition()));
			Assert.IsFalse(EditorUtility.IsDirty(asset));

			Level a = asset.CreateLevel();
			Level b = asset.CreateLevel();
			for (int i = 0; i < a.Tiles.Count; i++)
			{
				Assert.AreNotSame(a.Tiles[i], b.Tiles[i]);
			}
		}

		[TestCaseSource(nameof(LevelsAndSeeds))]
		public void RandomWalk_UndoRoundTrip(string name, int seed)
		{
			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(Load(name).CreateLevel(), undo);

			UndoRoundTrip.Check(sim, undo, RandomLevels.Moves(seed, 60));
		}

		#endregion

		#region Solutions

		[TestCaseSource(nameof(LevelNames))]
		public void RecordedSolution_ReplaysToCompleted(string name)
		{
			LevelData asset = Load(name);
			if (string.IsNullOrWhiteSpace(asset.Solution))
			{
				Assert.Ignore("no recorded solution: S1/S2");
			}

			AssertSolves(asset, asset.Solution);
		}

		[TestCase("Level03", "RRRULLLURRRULLL")]
		[TestCase(SpringTest, "URURLL")]
		public void GoldenSolution_IsRecorded_AndSolves(string name, string golden)
		{
			LevelData asset = Load(name);

			AssertSolves(asset, golden);
			Assert.AreEqual(golden, asset.Solution);
		}

		/// <summary>
		/// Every move is a real move, the level stays Playing until the last one completes it, and
		/// undoing everything restores the start.
		/// </summary>
		internal static void AssertSolves(LevelData asset, string solution)
		{
			Assert.IsTrue(MoveNotation.TryParse(solution, out IReadOnlyList<GridVector> moves), $"\"{solution}\" is not U/D/L/R");
			Assert.IsNotEmpty(moves);

			UndoSystem undo = new UndoSystem();
			LevelSimulation sim = new LevelSimulation(asset.CreateLevel(), undo);
			SimulationSnapshot start = sim.Snapshot();

			for (int i = 0; i < moves.Count; i++)
			{
				Assert.AreEqual(LevelStatus.Playing, sim.Status, $"before move {i + 1} of {moves.Count}");
				MoveResult result = sim.Move(moves[i]);
				Assert.AreEqual(MoveOutcome.Moved, result.Outcome, $"move {i + 1} ({MoveNotation.ToChar(moves[i])})");
			}

			Assert.AreEqual(LevelStatus.Completed, sim.Status, "after the last move");

			Assert.AreEqual(moves.Count, Sim.UndoAll(undo));
			Assert.AreEqual(start, sim.Snapshot());
		}

		#endregion
	}
}
