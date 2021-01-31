namespace Game.Levels
{
	internal readonly struct LevelInfo
	{
		public readonly IReadOnlyLevelData LevelData;
		public readonly bool Locked;

		public LevelInfo(IReadOnlyLevelData levelData, bool locked)
		{
			LevelData = levelData;
			Locked = locked;
		}
	}
}