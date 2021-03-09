using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BalsamicBits.Extensions;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(TilePrefabsManager), menuName = SONames.GameDir + nameof(TilePrefabsManager))]
	internal partial class TilePrefabsManager : SerializedScriptableObject
	{
		[SerializeField] private Dictionary<Type, GameObject> tilePrefabs;

		#region Unity

		private void Awake()
		{
#if UNITY_EDITOR
			UpdateDict();
#endif
		}

		#endregion

		#region API

		public BaseTileObject GetPrefabAndInstantiate(Tile tile)
		{
			Type tileObjectType;
			switch (tile)
			{
				case StoneTile _:
					tileObjectType = typeof(StoneTileObject);
					break;
				case WoodTile _:
					tileObjectType = typeof(WoodTileObject);
					break;
				case WaterTile _:
					tileObjectType = typeof(WaterTileObject);
					break;
				case GrassTile _:
					tileObjectType = typeof(GrassTileObject);
					break;
				default:
				case EmptyTile _:
					return null;
			}

			if (!tilePrefabs.TryGetValue(tileObjectType, out GameObject prefab))
			{
				Debug.LogError($"Prefab isn't set for {tileObjectType}");
				return null;
			}

			if (prefab == null)
			{
				return null;
			}
			
			GameObject tileGameObject = Instantiate(prefab);
			return tileGameObject.GetComponent(tileObjectType) as BaseTileObject;
		}

		#endregion

		#region Methods

		[Button("Update Dictionary Keys")]
		private void UpdateDict()
		{
			tilePrefabs ??= new SerialisedDictionary<Type, GameObject>();

			Type[] types = Assembly.GetAssembly(typeof(BaseTileObject)).GetTypes();
			foreach (Type type in types
				.Where(t => t.IsSubclassOf(typeof(BaseTileObject))))
			{
				if (!tilePrefabs.ContainsKey(type))
				{
					tilePrefabs.Add(type, null);
				}
			}

			foreach (Type key in tilePrefabs.Keys.ToArray())
			{
				if (!key.IsSubclassOf(typeof(BaseTileObject)))
				{
					tilePrefabs.Remove(key);
				}
			}
		}

		#endregion
	}
}