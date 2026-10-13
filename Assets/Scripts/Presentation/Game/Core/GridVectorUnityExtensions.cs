using UnityEngine;

namespace Game.Core
{
	/// <summary>Unity conversions for the engine-free <see cref="GridVector"/>.</summary>
	public static class GridVectorUnityExtensions
	{
		public static Vector3 ToXZ(this GridVector vector) => new Vector3(vector.x, 0, vector.y);

		public static Vector2Int ToVector2Int(this GridVector vector) => new Vector2Int(vector.x, vector.y);

		public static Vector2 ToVector2(this GridVector vector) => new Vector2(vector.x, vector.y);

		public static GridVector ToGridVector(this Vector2Int vector) => new GridVector(vector.x, vector.y);

		public static GridVector RoundToGridVector(this Vector2 vector) =>
			new GridVector(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
	}
}
