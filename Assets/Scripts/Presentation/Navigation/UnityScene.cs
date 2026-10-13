namespace Core
{
	/// <summary>
	/// The app's scenes. <see cref="ViewManager"/> loads each by its scene name (the enum name), so the values are
	/// not build indices and the Build Settings order doesn't matter. Values are serialized (ViewManager.asset); keep them.
	/// </summary>
	public enum UnityScene
	{
		None = -1,
		LevelSelect = 0,
		Game = 1,
		MainMenu = 2,
	}
}
