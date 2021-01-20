using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Core;
using Game.Core;
using Game.Tiles;
using Newtonsoft.Json;
using Sirenix.Serialization;
using UnityEngine.Serialization;
using Utils;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = "LevelData", menuName = SONames.GameDir + "Level Data")]
	internal partial class LevelData : SerializedScriptableObject, IReadOnlyLevelData
	{
		[SerializeField] private Guid id;

		[SerializeField, HideInInspector] private GridVector startPosition = new GridVector(1, 1);

		[OdinSerialize]
#if UNITY_EDITOR
		[TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawColouredTileElement), HideColumnIndices = true,
			HideRowIndices = true)]
#endif
		internal Tile[,] newTiles;

		// TODO
		// [ShowInInspector] private InspectorLevelDataWrapper tilesWrapper = new InspectorLevelDataWrapper();

		[ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty]
		public int Width => newTiles.GetLength(0);

		[ShowInInspector, PropertyOrder(1), MinValue(1), DelayedProperty]
		public int Depth => newTiles.GetLength(1);

		[ShowInInspector, PropertyOrder(1), DelayedProperty]
		public GridVector StartPosition => startPosition;

		// keep just in case Odin serialiser messes up
		[FormerlySerializedAs("tilesData")] [SerializeField, TextArea(8, 12)]
		private string ___tilesData;

		[FormerlySerializedAs("tiles")] [SerializeField]
		private Serialised2dArray<TileData> ___tiles;

		public Guid Id => id;

		private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.Auto,
			NullValueHandling = NullValueHandling.Ignore
		};

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

		public Tile GetTile(int x, int y)
		{
			return newTiles[x, y];
		}

		public Tile GetTile(GridVector position)
		{
			return GetTile(position.x, position.y);
		}

		public static LevelData CreateFrom(IReadOnlyLevelData input)
		{
			LevelData output = CreateInstance<LevelData>();

			output.newTiles = new Tile[input.Width, input.Depth];
			Loops.TwoD(input.Width, input.Depth, (x, y) => output.newTiles[x, y] = input.GetTile(x, y).Clone());

			output.startPosition = input.StartPosition;

			output.id = Guid.NewGuid();

			return output;
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return newTiles.Cast<Tile>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			yield return GetEnumerator();
		}

		#endregion

		protected override void OnBeforeSerialize()
		{
			___tilesData = JsonConvert.SerializeObject(newTiles, _jsonSettings);
		}

		protected override void OnAfterDeserialize()
		{
			if (string.IsNullOrEmpty(___tilesData))
			{
				// TODO
				RecreateDataFromArchived();
			}
			else
			{
				___tilesData = ___tilesData.Replace("Tilee", "Tile");
				newTiles = JsonConvert.DeserializeObject<Tile[,]>(___tilesData, _jsonSettings);
				___tilesData = null;
			}
		}

		[Button]
		private void RecreateDataFromArchived()
		{
			newTiles = new Tile[___tiles.Width, ___tiles.Depth];
			Loops.TwoD(___tiles.Width, ___tiles.Depth,
				(x, y) => newTiles[x, y] = TEMP_TileDataToTileeConverter.GetTilee(___tiles[x, y]));
		}
	}
}