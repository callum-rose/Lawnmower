#if UNITY_EDITOR
using UnityEngine;

namespace Game.Levels
{
	internal partial class LevelStateChecker
	{
		[ContextMenu("Debug/Fake Completed")]
		private void FakeCompleted() => LevelCompleted.Invoke(false);

		[ContextMenu("Debug/Fake Completed (inverted)")]
		private void FakeCompletedInverted() => LevelCompleted.Invoke(true);

		[ContextMenu("Debug/Fake Ruined")]
		private void FakeRuined() => LevelFailed.Invoke(false);

		[ContextMenu("Debug/Fake Ruined (inverted)")]
		private void FakeRuinedInverted() => LevelFailed.Invoke(true);
	}
}
#endif
