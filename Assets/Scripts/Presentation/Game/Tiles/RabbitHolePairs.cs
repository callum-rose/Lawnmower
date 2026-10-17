using UnityEngine;

namespace Game.Tiles
{
	/// <summary>
	/// The colour each rabbit hole pair is marked with: the carrot planted beside both of its holes. Palette colours
	/// from Assets/Docs/VoxelStyle.md, in the order generate_voxel_tiles.py lists them (CARROT_COLOURS).
	/// </summary>
	internal static class RabbitHolePairs
	{
		private static readonly Color[] Colours =
		{
			new Color32(0xf0, 0x82, 0x36, 0xff), // P44 Orange
			new Color32(0xb8, 0x68, 0xc0, 0xff), // P47 Purple
			new Color32(0xf6, 0xd0, 0x21, 0xff), // P42 Yellow
			new Color32(0xf6, 0xf6, 0xf2, 0xff), // P00 White
		};

		public static int Count => Colours.Length;

		/// <summary>Pairs past the last colour reuse them from the start.</summary>
		public static int Index(int pair) => ((pair % Count) + Count) % Count;

		public static Color Colour(int pair) => Colours[Index(pair)];
	}
}
