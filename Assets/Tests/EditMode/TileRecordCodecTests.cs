using System;
using Game.Core;
using Game.Levels.Serialization;
using Lawnmower.Model;
using NUnit.Framework;

namespace Tests.EditMode
{
	public class TileRecordCodecTests
	{
		private static readonly TileSpec[] AllSpecs =
		{
			TileSpec.Empty,
			TileSpec.Grass(0),
			TileSpec.Grass(1),
			TileSpec.Grass(2),
			TileSpec.Grass(3),
			TileSpec.Stone(3),
			TileSpec.Water,
			TileSpec.Wood,
			TileSpec.Spring(new GridVector(4, -2))
		};

		[Test]
		public void EveryTileConfiguration_RoundTrips()
		{
			foreach (TileSpec spec in AllSpecs)
			{
				TileRecord record = TileRecordCodec.FromSpec(spec);
				Assert.AreEqual(spec, TileRecordCodec.ToSpec(record), spec.ToString());
				Assert.AreEqual(spec, TileFactory.Create(spec).ToSpec(), spec.ToString());
			}
		}

		[Test]
		public void TileKind_ValuesAreStable()
		{
			// These numbers are persisted in level assets: never renumber them.
			Assert.AreEqual(0, (int)TileKind.Empty);
			Assert.AreEqual(1, (int)TileKind.Grass);
			Assert.AreEqual(2, (int)TileKind.Stone);
			Assert.AreEqual(3, (int)TileKind.Water);
			Assert.AreEqual(4, (int)TileKind.Wood);
			Assert.AreEqual(5, (int)TileKind.Spring);
		}

		[Test]
		public void Level_RoundTrips_RowMajor()
		{
			TileSpec[] specs =
			{
				TileSpec.Grass(1), TileSpec.Wood, TileSpec.Water,
				TileSpec.Stone(2), TileSpec.Empty, TileSpec.Spring(new GridVector(-1, -1))
			};
			LevelDefinition level = new LevelDefinition(Guid.NewGuid(), 3, 2, new GridVector(0, 0), specs);

			TileRecord[] records = TileRecordCodec.Encode(level);
			Assert.AreEqual(6, records.Length);
			// index = x + y * width: (x=1, y=0) is the wood tile
			Assert.AreEqual(TileKind.Wood, records[1].kind);

			LevelDefinition decoded = TileRecordCodec.Decode(level.Id, 3, 2, level.StartPosition, records);
			Assert.AreEqual(level, decoded);
			Assert.AreEqual(TileSpec.Stone(2), decoded[0, 1]);
		}

		[Test]
		public void Decode_WithWrongLength_Throws()
		{
			Assert.Throws<ArgumentException>(() => TileRecordCodec.Decode(Guid.Empty, 2, 2, default, new TileRecord[3]));
		}

		[Test]
		public void UnknownKind_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => TileRecordCodec.ToSpec(new TileRecord { kind = (TileKind)99 }));
		}
	}
}
