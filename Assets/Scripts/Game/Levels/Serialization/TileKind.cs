namespace Game.Levels.Serialization
{
	/// <summary>
	/// Persisted tile kind tag. The explicit values are what's written to level assets, so never
	/// renumber or reuse them; only append.
	/// </summary>
	internal enum TileKind
	{
		Empty = 0,
		Grass = 1,
		Stone = 2,
		Water = 3,
		Wood = 4,
		Spring = 5
	}
}
