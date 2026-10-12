using System;

namespace Game.Core
{
	[Serializable]
	public struct GridVector : IEquatable<GridVector>
	{
		public int x, y;

		public double Magnitude => Math.Sqrt(x * x + y * y);

		/// <summary>True for exactly Up, Down, Left or Right.</summary>
		public bool IsCardinalUnit => Math.Abs(x) + Math.Abs(y) == 1;

		public GridVector(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public static readonly GridVector Zero = new GridVector(0, 0);
		public static readonly GridVector Right = new GridVector(1, 0);
		public static readonly GridVector Left = new GridVector(-1, 0);
		public static readonly GridVector Up = new GridVector(0, 1);
		public static readonly GridVector Down = new GridVector(0, -1);

		public static GridVector operator +(GridVector coord1, GridVector coord2)
		{
			return new GridVector(coord1.x + coord2.x, coord1.y + coord2.y);
		}

		public static GridVector operator -(GridVector coord1, GridVector coord2)
		{
			return new GridVector(coord1.x - coord2.x, coord1.y - coord2.y);
		}

		public static GridVector operator -(GridVector coord2)
		{
			return new GridVector(-coord2.x, -coord2.y);
		}

		public static GridVector operator *(GridVector coord1, int factor)
		{
			return new GridVector(coord1.x * factor, coord1.y * factor);
		}

		public static bool operator ==(GridVector vector1, GridVector vector2)
		{
			return vector1.Equals(vector2);
		}

		public static bool operator !=(GridVector vector1, GridVector vector2)
		{
			return !(vector1 == vector2);
		}

		public override bool Equals(object obj)
		{
			return obj is GridVector other && Equals(other);
		}

		public bool Equals(GridVector other)
		{
			return x == other.x && y == other.y;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(x, y);
		}

		public override string ToString()
		{
			return $"[{x}, {y}]";
		}
	}
}
