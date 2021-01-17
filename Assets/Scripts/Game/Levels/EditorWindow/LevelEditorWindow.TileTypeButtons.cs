using Game.Tiles;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Game.Levels.EditorWindow
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
			protected override void DrawPropertyLayout(GUIContent label)
			{
				Color cachedColour = GUI.backgroundColor;
				
				foreach (Tile tile in TileeStatics.AllTileConfigurations)
				{
					string name = tile.ToString();
					Color colour = LevelEditorWindow.GetColourForTile(tile);

					GUI.backgroundColor = colour;
					if (GUILayout.Button(name))
					{
						ValueEntry.SmartValue.Window._currentTilePaint = tile;
					}
				}

				GUI.backgroundColor = cachedColour;
			}
		}
	}
}