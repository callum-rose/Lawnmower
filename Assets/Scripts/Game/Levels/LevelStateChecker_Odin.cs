using Sirenix.OdinInspector;

#if UNITY_EDITOR

namespace Game.Levels
{
	internal partial class LevelStateChecker
	{
		[Button(Expanded = true)]
		private void FakeCompleted(bool isInverted)
		{
			LevelCompleted.Invoke(isInverted);
		}
		
		[Button(Expanded = true)]
		private void FakeRuined(bool isInverted)
		{
			LevelFailed.Invoke(isInverted);
		}
	}
}

#endif