#if UNITY_EDITOR

namespace Game.Levels
{
	internal partial class LevelStateChecker
	{
		// Debug helpers (previously inspector buttons; call from code or the debugger).
		private void FakeCompleted(bool isInverted)
		{
			LevelCompleted.Invoke(isInverted);
		}

		private void FakeRuined(bool isInverted)
		{
			LevelFailed.Invoke(isInverted);
		}
	}
}

#endif
