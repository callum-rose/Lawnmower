using System;
using System.Collections.Generic;
using System.Text;
using Game.Levels;
using Game.Mowers;
using Lawnmower.Model;
using NUnit.Framework;
using UI.Buttons;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
	/// <summary>
	/// Fidelity gate for the Odin removal: the persisted level, mower and icon data must read back
	/// exactly as it did when Odin serialized it. Level and mower ids key player progress in PlayerPrefs.
	/// </summary>
	public class LevelAssetSnapshotTests
	{
		private const string LevelDir = "Assets/ScriptableObjects/Game/Levels/Data/";
		private const string MowerDir = "Assets/ScriptableObjects/Game/Mower/Data/";

		// name (path under LevelDir), id ("N" format; null = any non-empty id), size, start, rows (y = 0 first, "/"-separated)
		private static readonly object[][] Levels =
		{
			new object[]
			{
				"Level01", "78f25b51482c40ca888d9fa822cd2f16", "3x3", "0,0",
				"grass:1 grass:2 grass:2 / grass:2 empty grass:2 / grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Level02", "ea0d7938b56a4911a432a2cd89c5e57f", "3x4", "0,1",
				"grass:2 grass:2 empty / grass:1 grass:2 grass:2 / grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Level04", "14afdf4921cf43b8a7514fef8ebc9eef", "4x5", "2,3",
				"grass:2 grass:2 grass:2 grass:2 / grass:2 stone:1 grass:2 grass:2 / grass:2 stone:2 grass:2 grass:2 / grass:2 grass:2 grass:1 grass:2 / grass:2 grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Level07", "960d8c9e73d642699c260f17035210c4", "5x5", "2,1",
				"grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:1 grass:2 grass:2 / grass:2 grass:2 grass:2 stone:2 grass:2 / grass:2 stone:1 grass:2 spring:0,-2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Level08", "c620857b56494ab186fc315b118fbe97", "5x6", "3,4",
				"grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:3 / grass:2 grass:2 grass:2 grass:2 grass:2 / water water water water water / spring:0,-2 grass:2 grass:2 grass:1 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Level09", "62002a94fe2b4567a708e7f5274332d1", "5x6", "3,0",
				"grass:2 grass:2 grass:2 grass:1 grass:2 / spring:0,2 grass:2 grass:2 grass:2 grass:2 / water water water wood water / grass:2 grass:2 grass:2 grass:3 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:3 grass:2"
			},
			new object[]
			{
				"Level10", "dff15cf21bc04c40a84f269cdc3ac5d4", "6x6", "2,0",
				"grass:2 grass:2 grass:1 grass:2 grass:2 grass:2 / grass:2 grass:3 grass:3 spring:0,2 grass:2 grass:2 / water water water water water water / grass:2 grass:2 spring:0,-2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:3 grass:2 grass:2 grass:3 grass:2"
			},
			new object[]
			{
				"Archive/Level Spring Test", null, "3x3", "0,0",
				"grass:2 grass:2 grass:2 / grass:2 grass:2 empty / empty grass:2 spring:0,-2"
			},
			new object[]
			{
				"Level03", "3dbc7be08e344caa8b96bfda39edd55d", "4x4", "0,0",
				"grass:1 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Archive/Level3", "2b462ecb888545e588f62ebb524dd6e2", "7x7", "0,5",
				"grass:2 grass:2 grass:2 empty grass:2 grass:2 grass:2 / grass:2 stone:0 grass:2 empty grass:2 stone:1 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 / grass:2 stone:2 grass:2 stone:0 grass:2 stone:3 grass:2 / grass:1 grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 / empty grass:2 grass:2 grass:2 empty empty empty"
			},
			new object[]
			{
				"Level05", "8256fdfc0ed34892a0b20730d35495f6", "4x4", "0,2",
				"grass:2 grass:2 grass:2 grass:2 / grass:2 grass:3 grass:3 grass:2 / grass:2 grass:3 grass:3 grass:2 / grass:2 grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Archive/Level5", "9e55bac18995413da92713000191a911", "7x3", "1,1",
				"empty empty grass:2 grass:2 grass:2 empty empty / grass:2 grass:2 grass:3 grass:2 grass:3 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:1 grass:2 grass:2 grass:2"
			},
			new object[]
			{
				"Archive/Level6", "fb8d9ee4e5cf468fbe22978efe5cd85e", "13x9", "0,7",
				"empty empty empty empty grass:2 grass:2 grass:2 grass:2 grass:2 empty empty empty empty / empty empty empty empty grass:2 empty empty empty grass:2 empty grass:2 grass:2 grass:2 / empty empty empty empty grass:3 grass:2 grass:2 empty grass:2 grass:2 grass:2 grass:2 grass:2 / empty empty grass:2 grass:2 grass:3 empty grass:2 empty grass:2 grass:2 grass:2 grass:3 grass:2 / empty empty grass:2 empty grass:2 empty grass:2 empty grass:2 grass:2 grass:2 grass:2 empty / empty empty grass:2 empty grass:2 grass:2 grass:2 empty empty empty empty empty empty / empty empty grass:2 grass:2 grass:3 grass:3 grass:2 grass:2 empty empty empty empty empty / grass:1 grass:2 grass:2 empty grass:2 grass:2 empty grass:2 empty empty empty empty empty / empty empty grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 empty empty empty empty empty"
			},
			new object[]
			{
				"Archive/Level7", "8cef796429854b8fbac31b969e1b4243", "9x6", "2,3",
				"grass:2 grass:3 grass:2 empty empty empty empty empty empty / grass:2 grass:3 grass:2 wood grass:2 grass:2 grass:2 empty empty / empty grass:2 empty empty empty empty grass:2 grass:2 grass:2 / grass:2 grass:3 grass:2 wood grass:2 grass:2 grass:2 grass:3 grass:2 / grass:2 grass:2 grass:2 empty empty grass:2 grass:2 grass:2 empty / grass:2 grass:2 grass:2 empty empty empty empty empty empty"
			},
			new object[]
			{
				"Archive/Level8", "f9298d8f0f944a23ac58a24bf8c3c380", "14x9", "2,1",
				"grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 grass:2 wood wood wood grass:2 grass:2 empty empty / grass:2 grass:2 grass:1 water water water water water water water water grass:2 empty empty / grass:2 grass:2 water water grass:3 grass:3 wood wood water grass:2 grass:2 grass:2 empty empty / grass:2 grass:2 grass:2 grass:2 grass:3 grass:2 empty wood wood grass:3 empty empty empty empty / grass:2 empty empty grass:2 grass:2 grass:2 empty empty empty grass:2 empty grass:2 grass:2 grass:2 / grass:2 grass:2 grass:2 grass:2 grass:2 grass:3 wood wood empty grass:2 grass:2 grass:2 empty grass:2 / empty empty empty empty empty empty empty wood wood grass:3 grass:2 empty grass:2 grass:2 / empty empty empty empty empty empty empty empty empty grass:2 grass:2 grass:2 grass:2 empty / empty empty empty empty empty empty empty empty empty grass:2 empty empty empty empty"
			},
			new object[]
			{
				"Level06", "0fbbe24b992942c187861eb16870e6a1", "5x5", "4,2",
				"empty grass:2 grass:2 grass:2 grass:2 / empty grass:2 grass:3 grass:2 grass:2 / grass:2 grass:3 wood grass:3 grass:1 / grass:2 grass:2 wood wood empty / grass:2 grass:2 grass:2 grass:2 empty"
			},
		};

		private static IEnumerable<TestCaseData> LevelCases()
		{
			foreach (object[] level in Levels)
			{
				yield return new TestCaseData(level).SetName($"Level_{((string)level[0]).Replace(' ', '_').Replace('/', '_')}");
			}
		}

		[TestCaseSource(nameof(LevelCases))]
		public void LevelAsset_MatchesSnapshot(string name, string id, string size, string start, string rows)
		{
			LevelData asset = AssetDatabase.LoadAssetAtPath<LevelData>(LevelDir + name + ".asset");
			Assert.IsNotNull(asset, name);
			LevelDefinition level = asset.ToDefinition();

			if (id == null)
			{
				Assert.AreNotEqual(Guid.Empty, level.Id, "id must not be empty");
			}
			else
			{
				Assert.AreEqual(id, level.Id.ToString("N"), "id");
			}

			Assert.AreEqual(size, $"{level.Width}x{level.Depth}", "size");
			Assert.AreEqual(start, $"{level.StartPosition.x},{level.StartPosition.y}", "start");
			Assert.AreEqual(rows, Describe(level), "tiles");
		}

		[Test]
		public void LevelAsset_IdIsStableAcrossLoads()
		{
			// Ids must be persisted, not regenerated on load (Spring Test used to have an empty
			// persisted id that Awake replaced with a random one every domain reload).
			foreach (string guid in AssetDatabase.FindAssets("t:LevelData", new[] { "Assets/ScriptableObjects" }))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				SerializedObject so = new SerializedObject(AssetDatabase.LoadAssetAtPath<LevelData>(path));
				SerializedProperty prop = so.FindProperty("guid");
				if (prop == null)
				{
					Assert.Inconclusive("Level data not migrated yet");
				}

				Assert.IsTrue(Guid.TryParse(prop.stringValue, out Guid parsed) && parsed != Guid.Empty, path);
			}
		}

		[TestCase("EggBunny", "344dbd1651b546189959dd71f623276d")]
		[TestCase("PatchworkBunny", "74c3a9e4249b4da9a47b171f5bd88dc0")]
		[TestCase("RegularBunny", "57944479062546d9bab013ca63cb41f6")]
		[TestCase("RoboBunny", "f7536016d50d4cbd8d3016edd0422253")]
		[TestCase("RoboSnail", "42f1c3d3cf164371ac4ec62a81a89296")]
		[TestCase("SandyBunny", "5b29802830164088898299586da44fd1")]
		public void MowerAsset_HasPersistedId(string name, string id)
		{
			MowerData mower = AssetDatabase.LoadAssetAtPath<MowerData>(MowerDir + name + ".asset");
			Assert.IsNotNull(mower, name);
			Assert.AreEqual(id, mower.Id.ToString("N"));
		}

		[TestCase(IconType.None, null)]
		[TestCase(IconType.Back, "c1b1b162dad188f4da28e13a932928aa")]
		[TestCase(IconType.Exit, "ca5518e02a5502c488c87f645e432c55")]
		[TestCase(IconType.Play, "62f161907da906c40952cad1eeac46f2")]
		[TestCase(IconType.Undo, "4b91353526fff7c42a0b9ce3dc088c0c")]
		public void IconDataHolder_MapsIconToSprite(IconType icon, string spriteGuid)
		{
			IconDataHolder holder = AssetDatabase.LoadAssetAtPath<IconDataHolder>("Assets/ScriptableObjects/UI/IconDataHolder.asset");
			Assert.IsNotNull(holder);

			Sprite sprite = holder.GetIcon(icon);
			if (spriteGuid == null)
			{
				Assert.IsNull(sprite);
				return;
			}

			Assert.IsNotNull(sprite, icon.ToString());
			Assert.AreEqual(spriteGuid, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite)));
		}

		private static string Describe(LevelDefinition level)
		{
			StringBuilder sb = new StringBuilder();
			for (int y = 0; y < level.Depth; y++)
			{
				if (y > 0)
				{
					sb.Append(" / ");
				}

				for (int x = 0; x < level.Width; x++)
				{
					if (x > 0)
					{
						sb.Append(' ');
					}

					sb.Append(level[x, y].ToString());
				}
			}

			return sb.ToString();
		}
	}
}
