using System;
using UnityEngine;

namespace Game.Tiles
{
	[Serializable]
	public struct GrassTileSetupData
	{
		[Range(0, GrassTile.MaxGrassHeight)] public int grassHeight;
		
		public GrassTileSetupData(int grassHeight)
		{
			this.grassHeight = grassHeight;
		}
	}
}