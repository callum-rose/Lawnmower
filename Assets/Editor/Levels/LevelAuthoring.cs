using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Game.Core;
using Game.Levels;
using Lawnmower.Model;
using UnityEditor;
using UnityEngine;

namespace Lawnmower.Editor.Levels
{
	/// <summary>
	/// The Bunny Heaven level list, authored in code. The new levels were found by generate-and-test
	/// against <c>Lawnmower.Solver</c> (see Assets/Docs/LEVELS.md); the kept ones are existing assets.
	/// "Bunny Heaven/Author Levels" creates or updates the new level assets (guids are fixed here, so
	/// re-running never changes progress keys) and writes the level list in curve order. Run
	/// "Bunny Heaven/Recompute Par" afterwards to record par and a shortest solution.
	/// </summary>
	public static class LevelAuthoring
	{
		public const string DataDir = "Assets/ScriptableObjects/Game/Levels/Data";
		public const string ManagerPath = "Assets/ScriptableObjects/Game/Levels/Functionals/LevelDataManager.asset";

		/// <summary>A level written by this script. Rows run from y = 0 upward, separated by "/".</summary>
		private sealed class Authored
		{
			public string Name;
			public string Guid;
			public GridVector Start;
			public string Rows;
		}

		// Tokens: grass:N, stone:D, water, wood, empty, spring:dx,dy (relative landing offset), hole:N (rabbit hole pair N).
		private static readonly Authored[] AuthoredLevels =
		{
			// 1. Mow every tile once: a ring round a flower bed.
			new Authored { Name = "Level01", Guid = "78f25b51482c40ca888d9fa822cd2f16", Start = new GridVector(0, 0),
				Rows = "grass:1 grass:2 grass:2 / grass:2 empty grass:2 / grass:2 grass:2 grass:2" },
			// 2. Mowed grass can't be crossed again, so the first move matters (2 of 3 openings are dead ends).
			new Authored { Name = "Level02", Guid = "ea0d7938b56a4911a432a2cd89c5e57f", Start = new GridVector(0, 1),
				Rows = "grass:2 grass:2 empty / grass:1 grass:2 grass:2 / grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2" },
			// 4. Stones block the mower: route around them.
			new Authored { Name = "Level04", Guid = "14afdf4921cf43b8a7514fef8ebc9eef", Start = new GridVector(2, 3),
				Rows = "grass:2 grass:2 grass:2 grass:2 / grass:2 stone:1 grass:2 grass:2 / grass:2 stone:2 grass:2 grass:2 / grass:2 grass:2 grass:1 grass:2 / grass:2 grass:2 grass:2 grass:2" },
			// 7. A spring jumps you over the stone to the tile you'd otherwise strand.
			new Authored { Name = "Level07", Guid = "960d8c9e73d642699c260f17035210c4", Start = new GridVector(2, 1),
				Rows = "grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:1 grass:2 grass:2 / grass:2 grass:2 grass:2 stone:2 grass:2 / grass:2 stone:1 grass:2 spring:0,-2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2" },
			// 8. Water splits the lawn and the spring only goes one way: finish this bank first.
			new Authored { Name = "Level08", Guid = "c620857b56494ab186fc315b118fbe97", Start = new GridVector(3, 4),
				Rows = "grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:3 / grass:2 grass:2 grass:2 grass:2 grass:2 / water water water water water / spring:0,-2 grass:2 grass:2 grass:1 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2" },
			// 9. A wooden bridge can be crossed both ways, any number of times; the spring can't.
			new Authored { Name = "Level09", Guid = "62002a94fe2b4567a708e7f5274332d1", Start = new GridVector(3, 0),
				Rows = "grass:2 grass:2 grass:2 grass:1 grass:2 / spring:0,2 grass:2 grass:2 grass:2 grass:2 / water water water wood water / grass:2 grass:2 grass:2 grass:3 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:3 grass:2" },
			// 10. Two springs, one each way: cross the river and come back.
			new Authored { Name = "Level10", Guid = "dff15cf21bc04c40a84f269cdc3ac5d4", Start = new GridVector(2, 0),
				Rows = "grass:2 grass:2 grass:1 grass:2 grass:2 grass:2 / grass:2 grass:3 grass:3 spring:0,2 grass:2 grass:2 / water water water water water water / grass:2 grass:2 spring:0,-2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:3 grass:2 grass:2 grass:3 grass:2" },
			// 11. A pair of rabbit holes: in one, out of the other, still heading the same way.
			new Authored { Name = "Level11", Guid = "d163392839bf4f77ac64410fd5dcc262", Start = new GridVector(4, 2),
				Rows = "grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 hole:0 grass:2 grass:1 / water grass:2 grass:2 grass:2 grass:2 / grass:2 hole:0 grass:2 grass:3 grass:2" },
		};

		/// <summary>The level list, in play order. Names are asset file names under <see cref="DataDir"/>.</summary>
		public static readonly string[] LevelOrder =
		{
			"Level01", "Level02",
			"Level03", // was Level1
			"Level04",
			"Level05", // was Level4
			"Level06", // was Level9
			"Level07", "Level08", "Level09", "Level10",
			"Level11",
		};

		[MenuItem("Bunny Heaven/Author Levels")]
		public static void AuthorMenu() => Run();

		/// <summary>Also callable from CLI eval. Returns a one-line summary per level.</summary>
		public static string Run()
		{
			List<string> log = new List<string>();

			foreach (Authored authored in AuthoredLevels)
			{
				string path = $"{DataDir}/{authored.Name}.asset";
				LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(path);
				bool created = asset == null;
				if (created)
				{
					asset = ScriptableObject.CreateInstance<LevelData>();
					AssetDatabase.CreateAsset(asset, path);
				}

				SerializedObject so = new SerializedObject(asset);
				SerializedProperty guid = so.FindProperty("guid");
				// A new asset gets a random id from LevelData.Reset; replace it with the fixed one.
				if (!created && !string.IsNullOrEmpty(guid.stringValue) && guid.stringValue != authored.Guid)
				{
					throw new InvalidOperationException($"{path} already has id {guid.stringValue}, expected {authored.Guid}");
				}

				guid.stringValue = authored.Guid;
				so.ApplyModifiedPropertiesWithoutUndo();

				LevelDefinition definition = Parse(System.Guid.ParseExact(authored.Guid, "N"), authored.Rows, authored.Start);
				if (created || !definition.Equals(asset.ToDefinition()))
				{
					asset.SetContents(definition);
					// The layout changed, so any recorded solution and par are stale.
					so.Update();
					so.FindProperty("solution").stringValue = "";
					so.FindProperty("par").intValue = 0;
					so.ApplyModifiedPropertiesWithoutUndo();
					log.Add($"{authored.Name}: {(created ? "created" : "updated")} {definition.Width}x{definition.Depth}");
				}
				else
				{
					log.Add($"{authored.Name}: unchanged");
				}

				EditorUtility.SetDirty(asset);
			}

			LevelData[] ordered = LevelOrder
				.Select(n => AssetDatabase.LoadAssetAtPath<LevelData>($"{DataDir}/{n}.asset") ??
				             throw new InvalidOperationException($"No level asset {DataDir}/{n}.asset"))
				.ToArray();

			UnityEngine.Object manager = AssetDatabase.LoadMainAssetAtPath(ManagerPath);
			SerializedObject managerSo = new SerializedObject(manager);
			SerializedProperty list = managerSo.FindProperty("levelDatas");
			list.arraySize = ordered.Length;
			for (int i = 0; i < ordered.Length; i++)
			{
				list.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
			}

			managerSo.ApplyModifiedPropertiesWithoutUndo();
			EditorUtility.SetDirty(manager);
			AssetDatabase.SaveAssets();

			log.Add("Level list: " + string.Join(", ", LevelOrder));
			string text = string.Join("\n", log);
			Debug.Log("Author Levels:\n" + text);
			return text;
		}

		private static LevelDefinition Parse(Guid id, string rows, GridVector start)
		{
			string[][] grid = rows.Split('/')
				.Select(r => r.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
				.ToArray();

			int width = grid[0].Length;
			if (grid.Any(r => r.Length != width))
			{
				throw new FormatException($"Rows have different widths in \"{rows}\"");
			}

			return new LevelDefinition(id, width, grid.Length, start, grid.SelectMany(r => r.Select(ParseTile)).ToArray());
		}

		private static TileSpec ParseTile(string token)
		{
			string[] parts = token.Split(':');
			switch (parts[0])
			{
				case "empty": return TileSpec.Empty;
				case "water": return TileSpec.Water;
				case "wood": return TileSpec.Wood;
				case "grass": return TileSpec.Grass(Int(parts[1]));
				case "stone": return TileSpec.Stone(Int(parts[1]));
				case "hole": return TileSpec.RabbitHole(Int(parts[1]));
				case "spring":
				{
					string[] xy = parts[1].Split(',');
					return TileSpec.Spring(new GridVector(Int(xy[0]), Int(xy[1])));
				}
				default: throw new FormatException($"Unknown tile token \"{token}\"");
			}
		}

		private static int Int(string s) => int.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
	}
}
