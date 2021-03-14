using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(TilePrefabsManager), menuName = SoNames.GameDir + nameof(TilePrefabsManager))]
	internal partial class TilePrefabsManager : ScriptableObject
	{
		[Serializable]
		private class StringGameObjectDictionary : SerialisedDictionary<string, GameObject>
		{
		}

		[SerializeField, ValidateInput(nameof(ValidatePrefabsDict))]
		private StringGameObjectDictionary tilePrefabs;

		private readonly IReadOnlyDictionary<Type, Type> _tileToObjectTypeDict = new Dictionary<Type, Type>()
		{
			{ typeof(StoneTile), typeof(StoneTileObject) },
			{ typeof(WoodTile), typeof(WoodTileObject) },
			{ typeof(WaterTile), typeof(WaterTileObject) },
			{ typeof(GrassTile), typeof(GrassTileObject) },
			{ typeof(EmptyTile), typeof(EmptyTileObject) },
			{ typeof(SpringTile), typeof(SpringTileObject) },
		};

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
			Type tileObjectType = _tileToObjectTypeDict[tile.GetType()];
			BaseTileObject prefab = GetPrefab(tileObjectType);

			if (prefab == null)
			{
				return null;
			}

			return Instantiate(prefab);
		}

		#endregion

		#region Methods

		[Button("Update Dictionary Keys")]
		private void UpdateDict()
		{
			//tilePrefabs ??= new StringGameObjectDictionary();

			Type[] types = Assembly.GetAssembly(typeof(BaseTileObject)).GetTypes();
			foreach (Type type in types
				.Where(t => t.IsSubclassOf(typeof(BaseTileObject))))
			{
				if (!tilePrefabs.ContainsKey(type.FullName))
				{
					tilePrefabs.Add(type.FullName, null);
				}
			}

			foreach (string key in tilePrefabs.Keys.ToArray())
			{
				Type type = GetTypeFrom(key);

				if (type == null)
				{
					Debug.LogError(key + " is not a type");
					continue;
				}

				if (!type.IsSubclassOf(typeof(BaseTileObject)))
				{
					tilePrefabs.Remove(key);
				}
			}
		}

		private BaseTileObject GetPrefab(Type tileObjectType)
		{
			if (tilePrefabs.TryGetValue(tileObjectType.FullName, out GameObject prefab) && prefab != null)
			{
				return prefab.GetComponent(tileObjectType) as BaseTileObject;
			}

			return null;
		}

		private static Type GetTypeFrom(string typeFullName)
		{
			return Type.GetType(typeFullName);
		}

		private bool ValidatePrefabsDict(StringGameObjectDictionary value)
		{
			foreach (KeyValuePair<string, GameObject> keyValuePair in value)
			{
				if (keyValuePair.Value == null)
				{
					continue;
				}

				string typeFullName = keyValuePair.Key;
				Type type = GetTypeFrom(typeFullName);
				BaseTileObject prefab = GetPrefab(type);

				if (prefab == null)
				{
					Debug.LogError($"Prefab does not have component {type.FullName}");
					return false;
				}
			}

			return true;
		}

		#endregion
	}
}