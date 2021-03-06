using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Core
{
	[Serializable]
	public struct GridVector : IEquatable<GridVector>
	{
		[HorizontalGroup(LabelWidth = 20)] public int x, y;

		public float Magnitude => ((Vector2Int) this).magnitude;

		public GridVector(int x, int y)
		{
			this.x = x;
			this.y = y;
		}

		public Vector3 ToXZ()
		{
			return new Vector3(x, 0, y);
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

		public static explicit operator GridVector(Vector2 vec)
		{
			return new GridVector(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
		}

		public static explicit operator Vector2(GridVector vec)
		{
			return new Vector2(vec.x, vec.y);
		}

		public static implicit operator Vector2Int(GridVector vec)
		{
			return new Vector2Int(vec.x, vec.y);
		}

		public static implicit operator GridVector(Vector2Int vec)
		{
			return new GridVector(vec.x, vec.y);
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
			return obj is GridVector && Equals((GridVector) obj);
		}

		public bool Equals(GridVector other)
		{
			return x == other.x && y == other.y;
		}

		public override string ToString()
		{
			return $"[{x}, {y}]";
		}
	}
}