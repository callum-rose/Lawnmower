using System;
using Game.Core;

namespace Game.Tiles
{
	[Serializable]
	internal class SpringTile : Tile, IReadonlySpringTile
	{
		public override bool IsComplete => true;
		
		public GridVector LandingPosition { get; set; }

		public override bool IsTraversable(bool editMode) => true;
	}
}