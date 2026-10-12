using System;
using Game.Tiles;

namespace Game.Levels.Serialization
{
	/// <summary>
	/// Maps between the runtime <see cref="Tile"/> hierarchy and the persisted <see cref="TileRecord"/>.
	/// Pure C#: no Unity serialization or CLR type names involved.
	/// </summary>
	internal static class TileRecordCodec
	{
		public static Tile ToTile(in TileRecord record)
		{
			switch (record.kind)
			{
				case TileKind.Empty:
					return new EmptyTile();
				case TileKind.Grass:
					return new GrassTile(record.grassHeight);
				case TileKind.Stone:
					return new StoneTile { Direction = record.stoneDirection };
				case TileKind.Water:
					return new WaterTile();
				case TileKind.Wood:
					return new WoodTile();
				case TileKind.Spring:
					return new SpringTile { LandingPosition = record.springLanding };
				default:
					throw new ArgumentOutOfRangeException(nameof(record), record.kind, "Unknown tile kind");
			}
		}

		public static TileRecord FromTile(Tile tile)
		{
			switch (tile)
			{
				case null:
					throw new ArgumentNullException(nameof(tile));
				case EmptyTile _:
					return new TileRecord { kind = TileKind.Empty };
				case GrassTile grass:
					return new TileRecord { kind = TileKind.Grass, grassHeight = grass.GrassHeight.Value };
				case StoneTile stone:
					return new TileRecord { kind = TileKind.Stone, stoneDirection = stone.Direction };
				case WaterTile _:
					return new TileRecord { kind = TileKind.Water };
				case WoodTile _:
					return new TileRecord { kind = TileKind.Wood };
				case SpringTile spring:
					return new TileRecord { kind = TileKind.Spring, springLanding = spring.LandingPosition };
				default:
					throw new ArgumentOutOfRangeException(nameof(tile), tile.GetType().Name, "Unknown tile type");
			}
		}

		/// <summary>Row-major: index = x + y * width.</summary>
		public static TileRecord[] Encode(Tile[,] tiles)
		{
			int width = tiles.GetLength(0);
			int depth = tiles.GetLength(1);
			TileRecord[] records = new TileRecord[width * depth];
			for (int y = 0; y < depth; y++)
			{
				for (int x = 0; x < width; x++)
				{
					records[x + y * width] = FromTile(tiles[x, y] ?? new EmptyTile());
				}
			}

			return records;
		}

		public static TileRecord[] Encode(IReadOnlyLevelData level)
		{
			TileRecord[] records = new TileRecord[level.Width * level.Depth];
			for (int y = 0; y < level.Depth; y++)
			{
				for (int x = 0; x < level.Width; x++)
				{
					records[x + y * level.Width] = FromTile(level.GetTile(x, y) ?? new EmptyTile());
				}
			}

			return records;
		}

		public static Tile[,] Decode(TileRecord[] records, int width, int depth)
		{
			records ??= Array.Empty<TileRecord>();
			if (records.Length != width * depth)
			{
				throw new ArgumentException($"Expected {width * depth} tile records for {width}x{depth}, got {records.Length}");
			}

			Tile[,] tiles = new Tile[width, depth];
			for (int y = 0; y < depth; y++)
			{
				for (int x = 0; x < width; x++)
				{
					tiles[x, y] = ToTile(records[x + y * width]);
				}
			}

			return tiles;
		}
	}
}
