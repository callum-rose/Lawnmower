namespace Game.Levels
{
	internal readonly struct LevelInfo
	{
		public readonly LevelData LevelData;
		public readonly bool Locked;

		public LevelInfo(LevelData levelData, bool locked)
		{
			LevelData = levelData;
			Locked = locked;
		}
	}
}
