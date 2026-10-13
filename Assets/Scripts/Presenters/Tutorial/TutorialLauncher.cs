namespace Lawnmower.Presenters
{
	/// <summary>
	/// Decides when the Game scene shows the tutorial: the first time Level 1 is played, or on the next Game load after
	/// <see cref="Request"/> (Main Menu "How to play", Settings "Replay tutorial").
	/// </summary>
	public static class TutorialLauncher
	{
		/// <summary>True until the next Game load consumes the request.</summary>
		public static bool IsRequested { get; private set; }

		/// <summary>Show the tutorial on the next Game load, whatever the level. The caller then loads the level (normally Level 1).</summary>
		public static void Request() => IsRequested = true;

		public static void CancelRequest() => IsRequested = false;

		/// <summary>Called once per Game load. Consumes a pending <see cref="Request"/>.</summary>
		public static bool ShouldShow(bool isFirstLevel, bool hasSeenTutorial)
		{
			bool show = IsRequested || (isFirstLevel && !hasSeenTutorial);
			IsRequested = false;
			return show;
		}
	}
}
