using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BalsamicBits.Extensions;
using Game.Core;
using Game.Levels;
using Game.Levels.Serialization;
using Game.Mowers;
using UI.Buttons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.Migration
{
	/// <summary>
	/// TEMPORARY (Odin removal, phase A). Copies Odin-only serialized data into the new
	/// Unity-serialized fields while Odin is still installed and can read it. Deleted in phase B.
	/// </summary>
	internal static class OdinDataMigration
	{
		private const string GameScenePath = "Assets/Scenes/Game.unity";

		[MenuItem("Lawnmower/Migrate Odin Data")]
		public static string Run()
		{
			StringBuilder log = new StringBuilder();

			foreach (LevelData level in Load<LevelData>("t:LevelData"))
			{
				Guid id = level.Id == Guid.Empty ? Guid.NewGuid() : level.Id;
				level.guid = id.ToString("N");
				level.width = level.Width;
				level.depth = level.Depth;
				level.tiles = TileRecordCodec.Encode(level.newTiles);
				EditorUtility.SetDirty(level);
				log.AppendLine($"Level {level.name}: {level.guid} {level.width}x{level.depth} ({level.tiles.Length} tiles)");
			}

			foreach (MowerData mower in Load<MowerData>("t:MowerData"))
			{
				if (mower.LegacyId == Guid.Empty)
				{
					throw new InvalidOperationException($"Mower {mower.name} has no id to migrate");
				}

				mower.guid = mower.LegacyId.ToString("N");
				EditorUtility.SetDirty(mower);
				log.AppendLine($"Mower {mower.name}: {mower.guid}");
			}

			foreach (IconDataHolder holder in Load<IconDataHolder>("t:IconDataHolder"))
			{
				holder.icons = EnumExtensions.GetValues<IconType>()
					.Select(i => new IconSprite
					{
						icon = i,
						sprite = holder.LegacyIcons != null && holder.LegacyIcons.TryGetValue(i, out Sprite s) ? s : null
					})
					.ToList();
				EditorUtility.SetDirty(holder);
				log.AppendLine($"Icons {holder.name}: {string.Join(", ", holder.icons.Select(i => $"{i.icon}={(i.sprite ? i.sprite.name : "null")}"))}");
			}

			AssetDatabase.SaveAssets();

			var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
			GestureForwardingHandler handler = UnityEngine.Object.FindAnyObjectByType<GestureForwardingHandler>(FindObjectsInactive.Include);
			bool[,] matrix = handler.LegacyMatrix;
			List<GestureForwardingHandler.GesturePair> pairs = new List<GestureForwardingHandler.GesturePair>();
			int n = matrix.GetLength(0);
			for (int a = 1; a < n; a++)
			{
				for (int b = a + 1; b < n; b++)
				{
					if (matrix[a, b])
					{
						pairs.Add(new GestureForwardingHandler.GesturePair
						{
							a = (GestureForwardingHandler.Gesture)a,
							b = (GestureForwardingHandler.Gesture)b
						});
					}
				}
			}

			handler.allowedGesturePairs = pairs;
			EditorUtility.SetDirty(handler);
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			log.AppendLine($"Gestures: {string.Join(", ", pairs.Select(p => $"({p.a},{p.b})"))}");

			Debug.Log("[OdinDataMigration]\n" + log);
			return log.ToString();
		}

		private static IEnumerable<T> Load<T>(string filter) where T : UnityEngine.Object =>
			AssetDatabase.FindAssets(filter, new[] { "Assets" })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<T>)
				.Where(o => o != null);
	}
}
