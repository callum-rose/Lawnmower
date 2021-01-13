using System;
using System.Linq;
using Game.Tiles;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Levels
{
	internal partial class LevelEditorWindow
	{
		private class InspectorLevelDataWrapperDrawer : OdinValueDrawer<InspectorLevelDataWrapper>
		{
			protected override void DrawPropertyLayout(GUIContent label)
			{
				InspectorLevelDataWrapper inspectorLevelDataWrapper = ValueEntry.SmartValue;
				LevelEditorWindow levelEditorWindow = inspectorLevelDataWrapper.Window;
				EditableLevelData levelData = levelEditorWindow._clonedCurrent;

				float guiWidth = GUILayoutUtility.GetAspectRect(Mathf.Infinity).width;

				const float axisIndicesSize = 30;
				float cellSize = (guiWidth - axisIndicesSize) / levelData.Width;

				Rect inspectorRect =
					GUILayoutUtility.GetAspectRect((levelData.Width * cellSize + axisIndicesSize) /
					                               (levelData.Depth * cellSize + axisIndicesSize));

				SubdivideRect(inspectorRect, new Vector2(axisIndicesSize, inspectorRect.height - axisIndicesSize),
					out Rect yAxisRect, out Rect levelRect, out Rect _, out Rect xAxisRect);

				DrawAxisLabels(levelData, xAxisRect, yAxisRect);
				DrawLevelGrid(levelData, levelRect, cellSize, levelEditorWindow);
			}

			private void DrawLevelGrid(EditableLevelData levelData, Rect levelRect, float cellSize,
				LevelEditorWindow levelEditorWindow)
			{
				int gridX = 0;
				int gridY = 0;
				foreach (int y in levelData.GetYIndices())
				{
					foreach (int x in levelData.GetXIndices())
					{
						Rect cellRect = GetCellRect(levelData, levelRect, cellSize, gridX, gridY);

						int levelY = GetLevelY(levelData.Depth, levelData.GetYIndices().First(), y);

						DrawTile(levelData, x, levelY, cellRect);

						if (x == levelEditorWindow.StartPosition.x && levelY == levelEditorWindow.StartPosition.y)
						{
							DrawStartPositionBanner(cellSize, cellRect);
						}

						if (Event.current.OnLeftClick(cellRect))
						{
							SetTile(levelEditorWindow, x, levelY);
						}

						gridX++;
					}

					gridY++;
					gridX = 0;
				}
			}

			private static Rect GetCellRect(EditableLevelData levelData, Rect levelRect, float cellSize, int x, int y)
			{
				int index = x + y * levelData.Width;
				Rect cellRect = levelRect.SplitTableGrid(levelData.Width, cellSize, index);
				return cellRect;
			}

			private static void DrawTile(EditableLevelData levelData, int x, int levelY, Rect cellRect)
			{
				Tile tileData = levelData.GetTile(x, levelY);
				Color tileColour = GetColourForTile(tileData);

				EditorGUI.DrawRect(cellRect.Padding(1), tileColour);
			}

			private static void DrawStartPositionBanner(float cellSize, Rect cellRect)
			{
				const int columnCount = 8;
				float rowHeight = cellSize / columnCount;
				for (int i = 0; i < columnCount * 2; i++)
				{
					EditorGUI.DrawRect(cellRect.SplitTableGrid(columnCount, rowHeight, i),
						(i + (int) ((float) i / columnCount) % 2) % 2 == 0 ? Color.black : Color.white);
				}
			}

			private static void SetTile(LevelEditorWindow levelEditorWindow, int x, int y)
			{
				levelEditorWindow.OnTileClicked(x, y);
			}

			private static void DrawAxisLabels(EditableLevelData levelData, Rect xAxisRect, Rect yAxisRect)
			{
				// draw x axis labels
				int gridI = 0;
				foreach (int i in levelData.GetXIndices())
				{
					Rect labelRect = xAxisRect.Split(gridI, levelData.Width);
					DrawIndexLabel(labelRect, i);

					gridI++;
				}

				// draw y axis labels
				int? minY = null;
				int gridJ = 0;
				foreach (int j in levelData.GetYIndices())
				{
					minY ??= j;
					Rect labelRect = yAxisRect.SplitVertical(gridJ, levelData.Depth);
					DrawIndexLabel(labelRect, GetLevelY(levelData.Depth, minY.Value, j));

					gridJ++;
				}

				static void DrawIndexLabel(Rect labelRect, int number)
				{
					GUIStyle guiStyle = new GUIStyle
					{
						alignment = TextAnchor.MiddleCenter,
						border = new RectOffset(1, 1, 1, 1)
					};
					EditorGUI.LabelField(labelRect, number.ToString(), guiStyle);
				}
			}

			private static int GetLevelY(int levelDepth, int levelYMin, int j)
			{
				return (levelDepth - 1 - (j - levelYMin)) + levelYMin;
			}

			/// <summary>
			/// Namings assume y axis is upwards
			/// </summary>
			private void SubdivideRect(Rect input, Vector2 relativeCutCenter,
				out Rect bottomLeft, out Rect bottomRight, out Rect topLeft, out Rect topRight)
			{
				Vector2 absoluteCutCenter = input.min + relativeCutCenter;

				bottomLeft = Rect.MinMaxRect(input.xMin, input.yMin, absoluteCutCenter.x, absoluteCutCenter.y);
				bottomRight = Rect.MinMaxRect(absoluteCutCenter.x, input.yMin, input.xMax, absoluteCutCenter.y);
				topLeft = Rect.MinMaxRect(input.xMin, absoluteCutCenter.y, absoluteCutCenter.x, input.yMax);
				topRight = Rect.MinMaxRect(absoluteCutCenter.x, absoluteCutCenter.y, input.xMax, input.yMax);
			}
		}

		private class InspectorLevelDataWrapper
		{
			internal readonly LevelEditorWindow Window;

			public InspectorLevelDataWrapper(LevelEditorWindow window)
			{
				Window = window;
			}
		}
	}
}