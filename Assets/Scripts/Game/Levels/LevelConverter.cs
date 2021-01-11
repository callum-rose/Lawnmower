using System;
using Core;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelConverter), menuName = SONames.GameDir + nameof(LevelConverter))]
	// TODO remove class
	internal class LevelConverter : ScriptableObject
	{
		[SerializeField, AssetsOnly] private TilePrefabsManager tilePrefabs;

		public void ConvertTilesToLevelData(LevelData input, ReadOnlyTiles tiles, GridVector mowerStartPosition)
		{
			throw new NotImplementedException();

			// input.Resize(tiles.Width, tiles.Depth);
			// input.StartPosition = mowerStartPosition;
			//
			// for (int y = 0; y < tiles.Depth; y++)
			// {
			// 	for (int x = 0; x < tiles.Width; x++)
			// 	{
			// 		Tilee tile = tiles.GetTile(x, y);
			// 		input.SetTile(x, y, tile);
			// 	}
			// }
		}

		public TileData ConvertTileToData(Tilee tile)
		{
			TileType type = tilePrefabs.GetTileTypeForTile(tile);
			object setupData = CreateDataFor(tile);
			return TileData.Factory.Create(type, setupData);
		}

		private static object CreateDataFor(Tilee tile)
		{
			switch (tile)
			{
				case GrassTile gt:
					return new GrassTileSetupData(gt.GrassHeight.Value);

				case StoneTile st:
				default:
					return null;
			}
		}
	}
}