using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game.Core;
using Game.Levels.Serialization;
using Game.Tiles;
using UnityEngine;

namespace Game.Levels
{
	/// <summary>
	/// A level asset. The tiles are persisted once, as Unity-serialized <see cref="TileRecord"/>s
	/// (row-major, index = x + y * width), and decoded lazily into runtime <see cref="Tile"/>s.
	/// </summary>
	[CreateAssetMenu(fileName = "LevelData", menuName = SoNames.GameDir + "Level Data")]
	internal partial class LevelData : ScriptableObject, IReadOnlyLevelData
	{
		/// <summary>Level id in Guid "N" format. Keys player progress, so never change it once shipped.</summary>
		[SerializeField] private string guid;

		[SerializeField] private GridVector startPosition = new GridVector(1, 1);

		[SerializeField, Min(1)] private int width = 1;
		[SerializeField, Min(1)] private int depth = 1;

		[SerializeField, HideInInspector] private TileRecord[] tiles = { new TileRecord() };

		[NonSerialized] private Tile[,] _cache;

		private Tile[,] Tiles => _cache ??= TileRecordCodec.Decode(tiles, width, depth);

		public Guid Id => Guid.TryParse(guid, out Guid g) ? g : Guid.Empty;

		public int Width => width;
		public int Depth => depth;
		public GridVector StartPosition => startPosition;

		#region Unity

		private void OnValidate()
		{
			_cache = null;
		}

#if UNITY_EDITOR
		private void Reset()
		{
			guid = Guid.NewGuid().ToString("N");
		}
#endif

		#endregion

		#region API

		public Tile GetTile(int x, int y)
		{
			return Tiles[x, y];
		}

		public Tile GetTile(GridVector position)
		{
			return GetTile(position.x, position.y);
		}

		/// <summary>Overwrites this level's contents (used by the level editor's save and by <see cref="CreateFrom"/>).</summary>
		internal void SetContents(IReadOnlyLevelData source, Guid id)
		{
			tiles = TileRecordCodec.Encode(source);
			width = source.Width;
			depth = source.Depth;
			startPosition = source.StartPosition;
			guid = id.ToString("N");
			_cache = null;
		}

		public static LevelData CreateFrom(IReadOnlyLevelData input)
		{
			LevelData output = CreateInstance<LevelData>();
			output.SetContents(input, input.Id);
			return output;
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return Tiles.Cast<Tile>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
