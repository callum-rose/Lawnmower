using Game.Core;

namespace Game.Tiles
{
	internal interface IReadonlySpringTile : IReadonlyTile
	{
		GridVector LandingPosition { get; }
	}
}