#if UNITY_EDITOR
using System;
using Game.Levels.EditorWindow;
using Game.Tiles;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

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

		private static Tile DrawColouredTileElement(Rect rect, Tile[,] arr, int x, int y)
		{
			_defaultEditorColour ??= GUI.color;

			int flippedY = arr.GetLength(1) - 1 - y;
			Tile value = arr[x, flippedY];

			Color colour = LevelEditorWindow.GetColourForTile(value);
			string text = "";
			switch (value)
			{
				case GrassTile grassTile:
					text = grassTile.GrassHeight.Value.ToString();
					break;
				case SpringTile springTile:
					text = springTile.LandingPosition.ToString();
					break;
			}

			EditorGUI.DrawRect(rect.Padding(1), colour);
			EditorGUI.LabelField(rect, text, new GUIStyle() { alignment = TextAnchor.MiddleCenter });

			if (x == arr.GetLength(0) - 1 && y == arr.GetLength(1) - 1)
			{
				GUI.color = _defaultEditorColour.Value;
			}

			return value;
		}
	}
}
#endif
