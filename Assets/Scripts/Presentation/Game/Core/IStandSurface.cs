namespace Game.Core
{
	/// <summary>A tile object the mower stands on top of rather than on the ground, e.g. the spring's mushroom cap.</summary>
	internal interface IStandSurface
	{
		/// <summary>Height of the surface above the tile's ground, in world units.</summary>
		float StandHeight { get; }
	}
}
