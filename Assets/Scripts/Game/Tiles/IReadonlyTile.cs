namespace Game.Tiles
{
	public interface IReadonlyTile
	{
		event TraverseEvent TraversedOnto;
		event TraverseEvent TraversedAway;
		event TraverseEvent BumpedInto;
	}
}