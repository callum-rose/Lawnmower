using System;

namespace Lawnmower.Presenters
{
	public enum TutorialStep
	{
		/// <summary>Swipe scheme: "Swipe to move the bunny".</summary>
		Swipe,

		/// <summary>Arrows scheme: "Tap an arrow to move the bunny".</summary>
		TapArrows,

		/// <summary>"Mow the rest of the garden!"; dismissed by the view (timer or tap).</summary>
		MowTheRest,
	}

	public interface ITutorialView
	{
		void ShowStep(TutorialStep step);
		void Hide();

		/// <summary>The player dismissed (or the view timed out) the current <see cref="TutorialStep.MowTheRest"/> step.</summary>
		event Action Dismissed;
	}
}
