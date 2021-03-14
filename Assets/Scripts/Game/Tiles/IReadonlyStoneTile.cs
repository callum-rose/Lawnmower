namespace Game.Tiles
{
	internal interface IReadonlyStoneTile : IReadonlyTile
	{
		int Direction { get; }
	}
}