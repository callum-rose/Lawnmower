namespace Game.Tiles
{
	public interface IReadonlyStoneTile : IReadonlyTile
	{
		int Direction { get; }
	}
}