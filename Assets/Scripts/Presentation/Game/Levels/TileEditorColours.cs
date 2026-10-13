using System;
using Lawnmower.Model;
using UnityEngine;

namespace Game.Levels
{
	/// <summary>Colours used to draw tiles in editor tooling (level editor window, LevelData inspector).</summary>
	internal static class TileEditorColours
	{
		public static Color Get(TileSpec tile)
		{
			Color colour;
			switch (tile.Kind)
			{
				case TileKind.Empty:
					colour = Color.white;
					break;

				case TileKind.Grass:
					switch (tile.GrassHeight)
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

				case TileKind.Stone:
					colour = Color.gray;
					break;

				case TileKind.Water:
					colour = Color.blue;
					break;

				case TileKind.Wood:
					ColorUtility.TryParseHtmlString("#916B4C", out colour);
					break;

				case TileKind.Spring:
					colour = Color.yellow;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return colour;
		}
	}
}
