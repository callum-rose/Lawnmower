using Sirenix.OdinInspector;
using System;
using System.Linq;
using UnityEngine;
using BalsamicBits.Extensions;
using Core;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(TilePrefabsManager), menuName = SONames.GameDir + nameof(TilePrefabsManager))]
	internal partial class TilePrefabsManager : SerializedScriptableObject
	{
		[SerializeField] private SerializedDictionary<TileType, GameObject> tilePrefabs;

		#region Unity

		private void Awake()
		{
			UpdateDict();
		}

		#endregion

		#region API

		public BaseTileObject GetPrefabAndInstantiate(Tile tile)
		{
			TileType tileType;
			Type tileObjectType;
			switch (tile)
			{
				case EmptyTile _:
					return null;
					// tileType = TileType.Empty;
					// tileObjectType = typeof(EmptyTileObject);
					// break;
				case StoneTile _:
					tileType = TileType.Stone;
					tileObjectType = typeof(StoneTileObject);
					break;
				case WoodTile _:
					tileType = TileType.Wood;
					tileObjectType = typeof(WoodTileObject);
					break;
				case WaterTile _:
					tileType = TileType.Water;
					tileObjectType = typeof(WaterTileObject);
					break;
				case GrassTile _:
					tileType = TileType.Grass;
					tileObjectType = typeof(GrassTileObject);
					break;
				default:
					throw new NotImplementedException();
			}

			GameObject tileGameObject = Instantiate(tilePrefabs[tileType]);
			return tileGameObject.GetComponent(tileObjectType) as BaseTileObject;
		}

		public TileType GetTileTypeForTile(Tile tile)
		{
			foreach (var kv in tilePrefabs)
			{
				if (kv.Value.GetType() == tile.GetType())
				{
					return kv.Key;
				}
			}

			throw new Exception($"{nameof(TileType)} not found for tile of type {tile.GetType()}");
		}

		#endregion

		#region Methods

		[Button("Update Dictionary Keys")]
		private void UpdateDict()
		{
			tilePrefabs ??= new SerializedDictionary<TileType, GameObject>();

			foreach (TileType t in EnumExtensions.GetValues<TileType>())
			{
				if (!tilePrefabs.ContainsKey(t))
				{
					tilePrefabs.Add(t, null);
				}
			}

			foreach (TileType key in tilePrefabs.Keys.ToArray())
			{
				if (!Enum.IsDefined(typeof(TileType), key))
				{
					tilePrefabs.Remove(key);
				}
			}
		}

		#endregion
	}
}