using System;
using Game.Core;
using Lawnmower.Model;

namespace Game.Levels.Serialization
{
	/// <summary>
	/// Maps between the persisted <see cref="TileRecord"/> and the model's <see cref="TileSpec"/>.
	/// Pure C#: no Unity serialization or CLR type names involved.
	/// </summary>
	internal static class TileRecordCodec
	{
		public static TileSpec ToSpec(in TileRecord record)
		{
			switch (record.kind)
			{
				case TileKind.Empty:
					return TileSpec.Empty;
				case TileKind.Grass:
					return TileSpec.Grass(record.grassHeight);
				case TileKind.Stone:
					return TileSpec.Stone(record.stoneDirection);
				case TileKind.Water:
					return TileSpec.Water;
				case TileKind.Wood:
					return TileSpec.Wood;
				case TileKind.Spring:
					return TileSpec.Spring(record.springOffset);
				case TileKind.RabbitHole:
					return TileSpec.RabbitHole(record.holePair);
				default:
					throw new ArgumentOutOfRangeException(nameof(record), record.kind, "Unknown tile kind");
			}
		}

		public static TileRecord FromSpec(in TileSpec spec)
		{
			switch (spec.Kind)
			{
				case TileKind.Empty:
				case TileKind.Water:
				case TileKind.Wood:
					return new TileRecord { kind = spec.Kind };
				case TileKind.Grass:
					return new TileRecord { kind = TileKind.Grass, grassHeight = spec.GrassHeight };
				case TileKind.Stone:
					return new TileRecord { kind = TileKind.Stone, stoneDirection = spec.StoneDirection };
				case TileKind.Spring:
					return new TileRecord { kind = TileKind.Spring, springOffset = spec.SpringOffset };
				case TileKind.RabbitHole:
					return new TileRecord { kind = TileKind.RabbitHole, holePair = spec.HolePair };
				default:
					throw new ArgumentOutOfRangeException(nameof(spec), spec.Kind, "Unknown tile kind");
			}
		}

		/// <summary>Row-major: index = x + y * width.</summary>
		public static TileRecord[] Encode(LevelDefinition level)
		{
			TileRecord[] records = new TileRecord[level.Tiles.Count];
			for (int i = 0; i < records.Length; i++)
			{
				records[i] = FromSpec(level.Tiles[i]);
			}

			return records;
		}

		public static LevelDefinition Decode(Guid id, int width, int depth, GridVector startPosition, TileRecord[] records)
		{
			records ??= Array.Empty<TileRecord>();
			if (records.Length != width * depth)
			{
				throw new ArgumentException($"Expected {width * depth} tile records for {width}x{depth}, got {records.Length}");
			}

			TileSpec[] specs = new TileSpec[records.Length];
			for (int i = 0; i < records.Length; i++)
			{
				specs[i] = ToSpec(records[i]);
			}

			return new LevelDefinition(id, width, depth, startPosition, specs);
		}
	}
}
