using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using BalsamicBits.Extensions;
using Core;
using Game.Core;
using Game.Tiles;
using Newtonsoft.Json;
using Sirenix.Serialization;
using Sirenix.Utilities;
using UnityEngine.Serialization;
using Utils;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = "LevelData", menuName = SONames.GameDir + "Level Data")]
	internal partial class LevelData : SerializedScriptableObject, ILevelData
	{
		[SerializeField] private Guid id;

		[SerializeField, HideInInspector] private GridVector startPosition = new GridVector(1, 1);

		[OdinSerialize,
		 TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawColouredTileElement), HideColumnIndices = true,
			 HideRowIndices = true)]
		internal Tilee[,] newTiles;

		[ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
		public int Width => newTiles.GetLength(0);

		[ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
		public int Depth => newTiles.GetLength(1);

		[ShowInInspector, PropertyOrder(1), DelayedProperty, OnValueChanged(nameof(ValidateStartPos))]
		public GridVector StartPosition
		{
			get => startPosition;
			set => startPosition = value;
		}

		// keep just in case Odin serialiser messes up
		[SerializeField, TextArea(8, 12)] private string tilesData;
		[SerializeField, HideInInspector] private Serialised2dArray<TileData> tiles;

		public Guid Id => id;

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

		public void Init(Tilee[,] tiles, GridVector mowerStartPosition)
		{
			StartPosition = mowerStartPosition;

			int width = tiles.GetLength(0);
			int depth = tiles.GetLength(1);

			newTiles = tiles;
			// this.tiles = new Serialised2dArray<TileData>(width, depth, tiles);
		}

		public Tilee GetTile(int x, int y)
		{
			return newTiles[x, y];
			// return tiles[x, y];
		}

		public Tilee GetTile(GridVector position)
		{
			return GetTile(position.x, position.y);
		}

		public void SetTile(int x, int y, Tilee tile)
		{
			newTiles[x, y] = tile;
			// tiles[x, y] = data;
		}

		public void SetTile(GridVector position, Tilee tile)
		{
			SetTile(position.x, position.y, tile);
		}

		// public void Resize(int newWidth, int newDepth)
		// {
		// 	if (newWidth <= 0 || newDepth <= 0)
		// 	{
		// 		return;
		// 	}
		//
		// 	Serialised2dArray<TileData> oldTiles = tiles;
		// 	tiles = null;
		//
		// 	// create new array
		// 	Serialised2dArray<TileData> newTiles = new Serialised2dArray<TileData>(newWidth, newDepth);
		// 	Utils.Loops.TwoD(newWidth, newDepth,
		// 		(x, y) => { newTiles[x, y] = new TileData(); });
		//
		// 	if (!oldTiles.IsNullOrEmpty())
		// 	{
		// 		// trim if new width is smaller
		// 		int xRange = Mathf.Min(newWidth, Width);
		// 		for (int x = 0; x < xRange; x++)
		// 		{
		// 			int yRange = Mathf.Min(newDepth, Depth);
		// 			for (int y = 0; y < yRange; y++)
		// 			{
		// 				newTiles[x, y] = oldTiles[x, y];
		// 			}
		// 		}
		// 	}
		//
		// 	// update values
		// 	tiles = newTiles;
		// }

		public IEnumerator<Tilee> GetEnumerator()
		{
			return newTiles.Cast<Tilee>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			yield return GetEnumerator();
		}

		#endregion

		protected override void OnBeforeSerialize()
		{
			JsonSerializerSettings settings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				NullValueHandling = NullValueHandling.Ignore
			};
			tilesData = JsonConvert.SerializeObject(newTiles, settings);
		}

		protected override void OnAfterDeserialize()
		{
			if (string.IsNullOrEmpty(tilesData))
			{
				newTiles = new Tilee[tiles.Width, tiles.Depth];
				Loops.TwoD(tiles.Width, tiles.Depth,
					(x, y) => newTiles[x, y] = TEMP_TileDataToTileeConverter.GetTilee(tiles[x, y]));
			}
			else
			{
				JsonSerializerSettings settings = new JsonSerializerSettings
				{
					TypeNameHandling = TypeNameHandling.Auto,
					NullValueHandling = NullValueHandling.Ignore
				};
				newTiles = JsonConvert.DeserializeObject<Tilee[,]>(tilesData, settings);
				tilesData = null;
			}
		}
	}
}