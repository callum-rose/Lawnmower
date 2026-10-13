namespace Core
{
	public static partial class PersistantData
	{
		public static class LevelModule
		{
			public static readonly IPersistentDataItem<bool> TutorialCompleted = new PlayerPrefsItem<bool>("tutorialCompleted", false);
		}
	}
}
