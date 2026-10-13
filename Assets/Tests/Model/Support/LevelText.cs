using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Game.Core;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// Levels as text, in the same grammar as LevelAssetSnapshotTests: rows from y = 0 upward,
	/// separated by "/"; tokens grass:N, stone:D, water, wood, empty, spring:dx,dy (relative offset).
	/// </summary>
	public static class LevelText
	{
		public static readonly Guid DefaultId = new Guid("0123456789abcdef0123456789abcdef");

		public static LevelDefinition Parse(string rows, GridVector start, Guid? id = null)
		{
			string[][] grid = rows.Split('/')
				.Select(r => r.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
				.ToArray();

			int width = grid[0].Length;
			if (grid.Any(r => r.Length != width))
			{
				throw new FormatException($"Rows have different widths in \"{rows}\"");
			}

			List<TileSpec> tiles = new List<TileSpec>();
			foreach (string[] row in grid)
			{
				tiles.AddRange(row.Select(ParseTile));
			}

			return new LevelDefinition(id ?? DefaultId, width, grid.Length, start, tiles);
		}

		public static TileSpec ParseTile(string token)
		{
			string[] parts = token.Split(':');
			switch (parts[0])
			{
				case "empty": return TileSpec.Empty;
				case "water": return TileSpec.Water;
				case "wood": return TileSpec.Wood;
				case "grass": return TileSpec.Grass(Int(parts[1]));
				case "stone": return TileSpec.Stone(Int(parts[1]));
				case "spring":
				{
					string[] xy = parts[1].Split(',');
					return TileSpec.Spring(new GridVector(Int(xy[0]), Int(xy[1])));
				}
				default: throw new FormatException($"Unknown tile token \"{token}\"");
			}
		}

		public static string Describe(LevelDefinition level)
		{
			StringBuilder sb = new StringBuilder();
			for (int y = 0; y < level.Depth; y++)
			{
				if (y > 0)
				{
					sb.Append(" / ");
				}

				for (int x = 0; x < level.Width; x++)
				{
					if (x > 0)
					{
						sb.Append(' ');
					}

					sb.Append(level[x, y].ToString());
				}
			}

			return sb.ToString();
		}

		private static int Int(string s) => int.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
	}
}
