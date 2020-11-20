using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using BalsamicBits.Extensions;
using Core;

namespace Game.Tiles
{
    [CreateAssetMenu(fileName = nameof(TilePrefabsHolder), menuName = SONames.GameDir + nameof(TilePrefabsHolder))]
    internal class TilePrefabsHolder : SerializedScriptableObject
    {
        [SerializeField] 
        private SerializedDictionary<TileType, Tile> tilePrefabs;

        #region Unity

        private void Awake()
        {
            UpdateDict();
        }

        #endregion

        #region API

        public Tile GetPrefab(TileType type)
        {
            if (!tilePrefabs.ContainsKey(type))
            {
                throw new KeyNotFoundException();
            }

            return tilePrefabs[type];
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

        #region Odin

        [Button("Update Dictionary Keys")]
        private void UpdateDict()
        {
            if (tilePrefabs == null)
            {
                tilePrefabs = new SerializedDictionary<TileType, Tile>();
            }

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
