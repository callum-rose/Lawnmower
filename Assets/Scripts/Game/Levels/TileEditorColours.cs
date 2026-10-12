using System;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	/// <summary>Colours used to draw tiles in editor tooling (level editor window, LevelData inspector).</summary>
	internal static class TileEditorColours
	{
		public static Color Get(Tile tileData)
		{
			Color colour;
			switch (tileData)
			{
				case EmptyTile _:
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

					break;

				case StoneTile _:
					colour = Color.gray;
					break;

				case WaterTile _:
					colour = Color.blue;
					break;

				case WoodTile _:
					ColorUtility.TryParseHtmlString("#916B4C", out colour);
					break;

				case SpringTile _:
					colour = Color.yellow;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return colour;
		}
	}
}
