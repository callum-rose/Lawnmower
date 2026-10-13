using System;
using System.Collections.Generic;
using System.Text;
using Game.Core;

namespace Lawnmower.Model
{
	/// <summary>Moves as text: U/D/L/R (Up = +y). Case and whitespace are ignored when parsing.</summary>
	public static class MoveNotation
	{
		public static IReadOnlyList<GridVector> Parse(string moves)
		{
			if (!TryParse(moves, out IReadOnlyList<GridVector> result, out char bad))
			{
				throw new FormatException($"Unknown move '{bad}' in \"{moves}\"");
			}

			return result;
		}

		public static bool TryParse(string moves, out IReadOnlyList<GridVector> result)
		{
			return TryParse(moves, out result, out _);
		}

		public static string Format(IEnumerable<GridVector> moves)
		{
			StringBuilder sb = new StringBuilder();
			foreach (GridVector move in moves)
			{
				sb.Append(ToChar(move));
			}

			return sb.ToString();
		}

		public static char ToChar(GridVector move)
		{
			if (move == GridVector.Up) return 'U';
			if (move == GridVector.Down) return 'D';
			if (move == GridVector.Left) return 'L';
			if (move == GridVector.Right) return 'R';
			throw new ArgumentException($"{move} is not a cardinal unit move", nameof(move));
		}

		private static bool TryParse(string moves, out IReadOnlyList<GridVector> result, out char bad)
		{
			List<GridVector> list = new List<GridVector>();
			result = list;
			bad = default;

			if (moves == null)
			{
				return true;
			}

			foreach (char c in moves)
			{
				if (char.IsWhiteSpace(c))
				{
					continue;
				}

				switch (char.ToUpperInvariant(c))
				{
					case 'U': list.Add(GridVector.Up); break;
					case 'D': list.Add(GridVector.Down); break;
					case 'L': list.Add(GridVector.Left); break;
					case 'R': list.Add(GridVector.Right); break;
					default:
						bad = c;
						result = null;
						return false;
				}
			}

			return true;
		}
	}
}
