using System;
using System.Collections.Generic;
using Core.EventChannels;
using Game.Core;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
    internal class LevelObjectFactory : MonoBehaviour
    {
        [SerializeField] private TileObjectFactory tileFactory;
        [SerializeField] private Positioner positioner;
        
        [TitleGroup("Event Channels")]
        [SerializeField] private GameObjectEventChannel tileCreatedEventChannel;
        [SerializeField] private GameObjectEventChannel tileWillBeDestroyedEventChannel;

        #region API

        public GameObject[,] Build(IReadOnlyLevelData level)
        {
            GameObject[,] tiles = new GameObject[level.Width, level.Depth];

            for (int y = 0; y < level.Depth; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    Tile tile = level.GetTile(x, y);
                    GameObject newTileObject = BuildAt(new GridVector(x, y), tile);
                    tiles[x, y] = newTileObject;
                }
            }

            return tiles;
        }
        
        #endregion
        
        #region Methods

        private GameObject BuildAt(GridVector position, Tile tile)
        {
            GameObject newTileObject = tileFactory.Create(tile);

            if (!newTileObject)
            {
                return newTileObject;
            }

            positioner.Position(newTileObject.transform, position);
#if UNITY_EDITOR
            newTileObject.name = "Tile " + position;
#endif

            tileCreatedEventChannel.Raise(newTileObject);

            return newTileObject;
        }

        public void Remove(IEnumerable<GameObject> tileObjects)
        {
            if (tileObjects == null)
            {
                return;
            }

            foreach (var t in tileObjects)
            {
                Remove(t);
            }
        }

        public void Remove(GameObject tileObject)
        {
            tileWillBeDestroyedEventChannel.Raise(tileObject);
            
            tileFactory.Remove(tileObject);
        }

        #endregion
    }
}
