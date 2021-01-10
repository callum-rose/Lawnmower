using System;
using Game.Tiles;
using Game.UndoSystem;
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
				LevelData levelData = levelEditorWindow._clonedCurrent;

				const float axisIndicesRelSize = 0.5f;
				Rect inspectorRect =
					GUILayoutUtility.GetAspectRect((levelData.Width + axisIndicesRelSize) /
					                               (levelData.Depth + axisIndicesRelSize));

				float cellSize = (inspectorRect.width * (levelData.Width / (levelData.Width + axisIndicesRelSize))) /
				                 levelData.Width;
				float axisIndicesSize = cellSize * axisIndicesRelSize;

				SubdivideRect(inspectorRect, new Vector2(axisIndicesSize, inspectorRect.height - axisIndicesSize),
					out Rect yAxisRect, out Rect levelRect, out Rect _, out Rect xAxisRect);

				DrawAxisLabels(levelData.Width, levelData.Depth, xAxisRect, yAxisRect);
				DrawLevelGrid(levelData, levelRect, cellSize, levelEditorWindow, inspectorLevelDataWrapper.UndoSystem);
			}

			private void DrawLevelGrid(LevelData levelData, Rect levelRect, float cellSize,
				LevelEditorWindow levelEditorWindow, IUndoSystem undoSystem)
			{
				for (int x = 0; x < levelData.Width; x++)
				{
					for (int y = 0; y < levelData.Depth; y++)
					{
						Rect cellRect = GetCellRect(levelData, levelRect, cellSize, x, y);

						int levelY = GetLevelY(levelData.Depth, y);

						DrawTile(levelData, x, levelY, cellRect);

						if (x == levelEditorWindow.StartPosition.x && levelY == levelEditorWindow.StartPosition.y)
						{
							DrawStartPositionBanner(cellSize, cellRect);
						}

						if (Event.current.OnLeftClick(cellRect))
						{
							SetTile(levelData, levelEditorWindow, x, levelY, undoSystem);
						}
					}
				}
			}

			private static Rect GetCellRect(LevelData levelData, Rect levelRect, float cellSize, int x, int y)
			{
				int index = x + y * levelData.Width;
				Rect cellRect = levelRect.SplitTableGrid(levelData.Width, cellSize, index);
				return cellRect;
			}

			private static void DrawTile(LevelData levelData, int x, int levelY, Rect cellRect)
			{
				TileData tileData = levelData.GetTile(x, levelY);
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
						(i + (int)((float) i / columnCount) % 2) % 2 == 0 ? Color.black : Color.white);
				}
			}

			private static void SetTile(LevelData levelData, LevelEditorWindow levelEditorWindow, int x, int y,
				IUndoSystem undoSystem)
			{
				TileData currentTile = levelData.GetTile(x, y);

				void Set_Local(TileData tileData)
				{
					levelData.SetTile(x, y, tileData);
				}

				IUndoable undoable = new Undoable(
					() => Set_Local(levelEditorWindow._tileData),
					() => Set_Local(currentTile));

				undoSystem.Do(undoable);
			}

			private static void DrawAxisLabels(int levelDataWidth, int levelDataDepth, Rect xAxisRect, Rect yAxisRect)
			{
				// draw x axis labels
				for (int i = 0; i < levelDataWidth; i++)
				{
					Rect labelRect = xAxisRect.Split(i, levelDataWidth);
					DrawIndexLabel(labelRect, i);
				}

				// draw y axis labels
				for (int j = 0; j < levelDataDepth; j++)
				{
					Rect labelRect = yAxisRect.SplitVertical(j, levelDataDepth);
					DrawIndexLabel(labelRect, GetLevelY(levelDataDepth, j));
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

			private static int GetLevelY(int levelDepth, int j)
			{
				return levelDepth - 1 - j;
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
			internal readonly IUndoSystem UndoSystem;

			public InspectorLevelDataWrapper(LevelEditorWindow window, IUndoSystem undoSystem)
			{
				Window = window;
				UndoSystem = undoSystem;
			}
		}
	}
}