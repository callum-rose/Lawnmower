namespace Lawnmower.Presenters
{
	/// <summary>Leaving the current level. The Unity implementation loads scenes.</summary>
	public interface IGameNavigator
	{
		bool HasNextLevel { get; }

		void LoadNextLevel();
		void RestartLevel();
		void LoadLevelSelect();
	}
}
