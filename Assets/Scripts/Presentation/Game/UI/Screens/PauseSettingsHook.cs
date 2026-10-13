using System;

namespace Game.UI.Screens
{
	/// <summary>
	/// PLACEHOLDER (wired at Final integration): Pause → SETTINGS. Set <see cref="Open"/> to show G4's Settings prefab
	/// over the Pause card; call the argument when Settings closes so the Pause card is shown again. While it is null the
	/// Game scene shows a "coming soon" toast.
	/// </summary>
	public static class PauseSettingsHook
	{
		/// <summary>Opens Settings; the argument is called when it closes.</summary>
		public static Action<Action> Open;
	}
}
