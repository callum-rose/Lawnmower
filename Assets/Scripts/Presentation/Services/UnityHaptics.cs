using Game.Haptics;
using Lawnmower.Presenters;

namespace Lawnmower.Services
{
	/// <summary>The iOS Taptic plugin (P3). Does nothing in the Editor and on other platforms.</summary>
	internal sealed class UnityHaptics : IHaptics
	{
		public void LightTap() => HapticFeedback.LightTap();
		public void Success() => HapticFeedback.Success();
		public void Error() => HapticFeedback.Error();
	}
}
