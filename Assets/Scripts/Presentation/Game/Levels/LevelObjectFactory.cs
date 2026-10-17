using System.Collections.Generic;
using Core.EventChannels;
using Game.Core;
using Game.Tiles;
using Lawnmower.Model;
using UnityEngine;

namespace Game.Levels
{
    internal class LevelObjectFactory : MonoBehaviour
    {
        [SerializeField] private TileObjectFactory tileFactory;
        [SerializeField] private Positioner positioner;
        
        [Header("Event Channels")]
        [SerializeField] private TileObjectEventChannel tileCreatedEventChannel;
        [SerializeField] private GameObjectEventChannel tileWillBeDestroyedEventChannel;

        #region API

        public GameObject[,] Build(Level level)
        {
            GameObject[,] tiles = new GameObject[level.Width, level.Depth];

            for (int y = 0; y < level.Depth; y++)
            {
                for (int x = 0; x < level.Width; x++)
                {
                    Tile tile = level.GetTile(x, y);
                    GameObject newTileObject = BuildAt(level, new GridVector(x, y), tile);
                    tiles[x, y] = newTileObject;
                }
            }

            return tiles;
        }
        
        #endregion
        
        #region Methods

        private GameObject BuildAt(Level level, GridVector position, Tile tile)
        {
            GameObject newTileObject = tileFactory.Create(tile);

            if (!newTileObject)
            {
                return newTileObject;
            }

            if (newTileObject.TryGetComponent(out WoodTileObject woodTileObject))
            {
                woodTileObject.Connect(GetWoodConnections(level, position));
            }

            if (newTileObject.TryGetComponent(out GrassEdgeBlades grassEdges))
            {
                grassEdges.Connect(level, position);
            }

            positioner.Position(newTileObject.transform, position);
#if UNITY_EDITOR
            newTileObject.name = "Tile " + position;
#endif

            tileCreatedEventChannel.Raise(newTileObject, position);

            return newTileObject;
        }

        private static WoodConnections GetWoodConnections(Level level, GridVector position)
        {
            WoodConnections connections = WoodConnections.None;

            if (IsWood(GridVector.Up)) connections |= WoodConnections.North;
            if (IsWood(GridVector.Right)) connections |= WoodConnections.East;
            if (IsWood(GridVector.Down)) connections |= WoodConnections.South;
            if (IsWood(GridVector.Left)) connections |= WoodConnections.West;

            return connections;

            bool IsWood(GridVector direction) => level.GetTile(position + direction)?.Kind == TileKind.Wood;
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

        private void Remove(GameObject tileObject)
        {
            tileWillBeDestroyedEventChannel.Raise(tileObject);
            
            tileFactory.Remove(tileObject);
        }

        #endregion
    }
}
