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

		private static Tilee DrawColouredTileElement(Rect rect, Tilee[,] arr, int x, int y)
		{
			_defaultEditorColour ??= GUI.color;

			int flippedY = arr.GetLength(1) - 1 - y;
			Tilee value = arr[x, flippedY];

			Color colour;
			string text = "";
			switch (value)
			{
				case EmptyTile emptyTile:
					colour = Color.white;
					break;
				case GrassTile grassTile:
					switch (grassTile.GrassHeight.Value)
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

					text = grassTile.GrassHeight.Value.ToString();
					break;
				case StoneTile stoneTile:
					colour = Color.gray;
					break;
				case WaterTile waterTile:
					colour = Color.blue;
					break;
				case WoodTile woodTile:
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