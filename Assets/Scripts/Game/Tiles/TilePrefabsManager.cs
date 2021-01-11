using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
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

		public BaseTileObject<T> GetPrefabAndInstantiate<T>(T tile) where T : Tilee
		{
			return GetPrefabAndInstantiate<T>();
		}

		public BaseTileObject<T> GetPrefabAndInstantiate<T>() where T : Tilee
		{
			GameObject prefab;
			Func<GameObject, BaseTileObject<T>> getComponentFunc;
			if (typeof(T) == typeof(EmptyTileObject))
			{
				prefab = tilePrefabs[TileType.Empty];
				getComponentFunc = g => g.GetComponent<EmptyTileObject>() as BaseTileObject<T>;
			}
			else if (typeof(T) == typeof(StoneTileObject))
			{
				prefab = tilePrefabs[TileType.Empty];
				getComponentFunc = g => g.GetComponent<StoneTileObject>() as BaseTileObject<T>;
			}
			else if (typeof(T) == typeof(WoodTileObject))
			{
				prefab = tilePrefabs[TileType.Empty];
				getComponentFunc = g => g.GetComponent<WoodTileObject>() as BaseTileObject<T>;
			}
			else if (typeof(T) == typeof(WaterTileObject))
			{
				prefab = tilePrefabs[TileType.Empty];
				getComponentFunc = g => g.GetComponent<WaterTileObject>() as BaseTileObject<T>;
			}
			else if (typeof(T) == typeof(GrassTileObject))
			{
				prefab = tilePrefabs[TileType.Empty];
				getComponentFunc = g => g.GetComponent<GrassTileObject>() as BaseTileObject<T>;
			}
			else
			{
				throw new NotImplementedException();
			}

			GameObject tileGameObject = Instantiate(prefab.gameObject);
			return getComponentFunc(tileGameObject);
		}

		public TileType GetTileTypeForTile(Tilee tile)
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