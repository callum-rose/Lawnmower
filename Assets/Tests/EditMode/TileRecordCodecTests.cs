using System;
using Game.Core;
using Game.Levels.Serialization;
using Game.Tiles;
using NUnit.Framework;

namespace Tests.EditMode
{
	public class TileRecordCodecTests
	{
		private static readonly Tile[] AllTiles =
		{
			new EmptyTile(),
			new GrassTile(0),
			new GrassTile(1),
			new GrassTile(2),
			new GrassTile(3),
			new StoneTile { Direction = 3 },
			new WaterTile(),
			new WoodTile(),
			new SpringTile { LandingPosition = new GridVector(4, -2) }
		};

		[Test]
		public void EveryTileConfiguration_RoundTrips()
		{
			foreach (Tile tile in AllTiles)
			{
				TileRecord record = TileRecordCodec.FromTile(tile);
				Tile decoded = TileRecordCodec.ToTile(record);

				Assert.AreEqual(tile.GetType(), decoded.GetType());
				Assert.AreEqual(tile.ConfigurationKey, decoded.ConfigurationKey);
				Assert.AreEqual(record, TileRecordCodec.FromTile(decoded));
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
		public void Grid_RoundTrips_RowMajor()
		{
			Tile[,] tiles =
			{
				{ new GrassTile(1), new StoneTile { Direction = 2 } },
				{ new WoodTile(), new EmptyTile() },
				{ new WaterTile(), new SpringTile { LandingPosition = new GridVector(1, 1) } }
			};

			TileRecord[] records = TileRecordCodec.Encode(tiles);
			Assert.AreEqual(6, records.Length);
			// index = x + y * width: (x=1, y=0) is the wood tile
			Assert.AreEqual(TileKind.Wood, records[1].kind);

			Tile[,] decoded = TileRecordCodec.Decode(records, 3, 2);
			for (int x = 0; x < 3; x++)
			{
				for (int y = 0; y < 2; y++)
				{
					Assert.AreEqual(TileRecordCodec.FromTile(tiles[x, y]), TileRecordCodec.FromTile(decoded[x, y]));
				}
			}
		}

		[Test]
		public void Decode_WithWrongLength_Throws()
		{
			Assert.Throws<ArgumentException>(() => TileRecordCodec.Decode(new TileRecord[3], 2, 2));
		}

		[Test]
		public void UnknownKind_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => TileRecordCodec.ToTile(new TileRecord { kind = (TileKind)99 }));
		}
	}
}
