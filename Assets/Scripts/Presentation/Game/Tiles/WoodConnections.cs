using System;

namespace Game.Tiles
{
	/// <summary>The edges of a wood tile that meet a neighbouring wood tile. Clockwise from north (+z).</summary>
	[Flags]
	internal enum WoodConnections
	{
		None = 0,
		North = 1 << 0,
		East = 1 << 1,
		South = 1 << 2,
		West = 1 << 3,
	}

	internal static class WoodConnectionsExtensions
	{
		private const int All = (int)(WoodConnections.North | WoodConnections.East | WoodConnections.South |
		                              WoodConnections.West);

		/// <summary>The connections after turning the tile <paramref name="quarterTurns"/> times clockwise, viewed from above.</summary>
		public static WoodConnections RotateClockwise(this WoodConnections connections, int quarterTurns)
		{
			int turns = ((quarterTurns % 4) + 4) % 4;
			int bits = (int)connections;
			return (WoodConnections)(((bits << turns) | (bits >> (4 - turns))) & All);
		}
	}
}
