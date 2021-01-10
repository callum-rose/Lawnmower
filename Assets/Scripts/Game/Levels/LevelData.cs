using System;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using BalsamicBits.Extensions;
using Core;
using Game.Core;
using Game.Tiles;
using Sirenix.Serialization;
using Sirenix.Utilities;
using Utils;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Game.Levels
{
	[CreateAssetMenu(fileName = "LevelData", menuName = SONames.GameDir + "Level Data")]
	internal partial class LevelData : SerializedScriptableObject, IReadOnlyLevelData
	{
		[SerializeField] private Guid id;

		[SerializeField, HideInInspector] private GridVector startPosition = new GridVector(1, 1);

		[SerializeField]
#if UNITY_EDITOR
		[PropertyOrder(2)]
#endif
		internal Serialised2dArray<TileData> tiles;

		[OdinSerialize,
		 TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawColouredTileElement), HideColumnIndices = true,
			 HideRowIndices = true)]
		internal TileData[,] test;

		[HideInInspector] public Guid Id => id;

#if UNITY_EDITOR
		[ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
#endif
		public int Width => tiles.Width;

#if UNITY_EDITOR
		[ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
#endif
		public int Depth => tiles.Depth;

#if UNITY_EDITOR
		[ShowInInspector, PropertyOrder(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
#endif
		public GridVector StartPosition
		{
			get => startPosition;
			set => startPosition = value;
		}

		#region Unity

		private void Awake()
		{
			if (id == Guid.Empty)
			{
				id = Guid.NewGuid();
			}
		}

		#endregion

		#region API

		public void Init(TileData[,] tiles, GridVector mowerStartPosition)
		{
			StartPosition = mowerStartPosition;

			int width = tiles.GetLength(0);
			int depth = tiles.GetLength(1);

			this.tiles = new Serialised2dArray<TileData>(width, depth, tiles);
		}

		public TileData GetTile(int x, int y)
		{
			return tiles[x, y];
		}

		public TileData GetTile(GridVector position)
		{
			return GetTile(position.x, position.y);
		}

		public void SetTile(int x, int y, TileData data)
		{
			tiles[x, y] = data;
		}

		public void SetTile(GridVector position, TileData data)
		{
			SetTile(position.x, position.y, data);
		}

		public void Resize(int newWidth, int newDepth)
		{
			if (newWidth <= 0 || newDepth <= 0)
			{
				return;
			}

			Serialised2dArray<TileData> oldTiles = tiles;
			tiles = null;

			// create new array
			Serialised2dArray<TileData> newTiles = new Serialised2dArray<TileData>(newWidth, newDepth);
			Utils.Loops.TwoD(newWidth, newDepth,
				(x, y) => { newTiles[x, y] = new TileData(); });

			if (!oldTiles.IsNullOrEmpty())
			{
				// trim if new width is smaller
				int xRange = Mathf.Min(newWidth, Width);
				for (int x = 0; x < xRange; x++)
				{
					int yRange = Mathf.Min(newDepth, Depth);
					for (int y = 0; y < yRange; y++)
					{
						newTiles[x, y] = oldTiles[x, y];
					}
				}
			}

			// update values
			tiles = newTiles;
		}

		#endregion
	}
}