using Game.Tiles;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.Levels
{
	internal partial class LevelEditorWindow
	{
		internal class TileTypeButtons
		{
			internal readonly LevelEditorWindow Window;

			public TileTypeButtons(LevelEditorWindow window)
			{
				Window = window;
			}
		}

		internal class TileTypeButtonsDrawer : OdinValueDrawer<TileTypeButtons>
		{
			private readonly TileData[] _possibleTileDatas = new TileData[]
			{
				TileData.Factory.Create(TileType.Empty, null),
				TileData.Factory.Create(TileType.Stone, null),
				TileData.Factory.Create(TileType.Wood, null),
				TileData.Factory.Create(TileType.Water, null),
				TileData.Factory.Create(TileType.Grass, new GrassTileSetupData(1)),
				TileData.Factory.Create(TileType.Grass, new GrassTileSetupData(2)),
				TileData.Factory.Create(TileType.Grass, new GrassTileSetupData(3)),
			};
			
			protected override void DrawPropertyLayout(GUIContent label)
			{
				Color cachedColour = GUI.backgroundColor;
				
				foreach (TileData data in _possibleTileDatas)
				{
					string name = data.ToString();
					Color colour = LevelEditorWindow.GetColourForTile(data);

					GUI.backgroundColor = colour;
					if (GUILayout.Button(name))
					{
						ValueEntry.SmartValue.Window._tileData = data;
					}
				}

				GUI.backgroundColor = cachedColour;
			}
		}
	}
}