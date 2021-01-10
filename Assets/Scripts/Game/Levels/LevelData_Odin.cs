using System;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using Utils;

namespace Game.Levels
{
	internal partial class LevelData
	{
		private static Color? _defaultEditorColour;
		
		[Button]
		internal void ConvertOldArrayTo2D()
		{
			test = new TileData[tiles.Width, tiles.Depth];
			Loops.TwoD(tiles.Width, tiles.Depth, (x, y) => { test[x, y] = tiles[x, y]; });
		}

		[Button]
		private void SelectInProjectWindow()
		{
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = this;
		}

		[Button]
		private void OpenLevelEditorWindow()
		{
			LevelEditorWindow.OpenWindow(this);
		}

		private static TileData DrawColouredTileElement(Rect rect, TileData[,] arr, int x, int y)
		{
			_defaultEditorColour ??= GUI.color;
			
			int flippedY = arr.GetLength(1) - 1 - y;
			TileData value = arr[x, flippedY];

			Color colour;
			string text = "";
			switch (value.Type)
			{
				case TileType.Empty:
					colour = Color.white;
					break;
				case TileType.Grass:
					GrassTileSetupData grassData = (GrassTileSetupData)value.Data;
					switch (grassData.grassHeight)
					{
						case 3:
							ColorUtility.TryParseHtmlString("#0D7352", out colour);
							break;
						case 2:
							ColorUtility.TryParseHtmlString("#12A175", out colour);
							break;
						case 1:
							ColorUtility.TryParseHtmlString("#1BF1AC", out colour);
							break;
						default:
							colour = Color.black;
							break;
					}

					text = grassData.grassHeight.ToString();

					break;
				case TileType.Stone:
					colour = Color.gray;
					break;
				case TileType.Water:
					colour = Color.blue;
					break;
				case TileType.Wood:
					ColorUtility.TryParseHtmlString("#916B4C", out colour);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			EditorGUI.DrawRect(rect.Padding(1), colour);
			EditorGUI.LabelField(rect, text, new GUIStyle() { alignment = TextAnchor.MiddleCenter });

			if (x == arr.GetLength(0) - 1 && y == arr.GetLength(1) - 1)
			{
				GUI.color = _defaultEditorColour.Value;
			}
			
			return value;
		}

		internal void ValidateStartPos()
		{
			int x = Mathf.Min(Width - 1, Mathf.Max(0, StartPosition.x));
			int y = Mathf.Min(Depth - 1, Mathf.Max(0, StartPosition.y));
			StartPosition = new GridVector(x, y);
		}
	}
}