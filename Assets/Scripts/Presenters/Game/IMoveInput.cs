using System;
using Game.Core;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// A source of move requests (keyboard, swipe, on-screen arrows). The only way moves reach
	/// <see cref="GamePresenter"/>. Step 6 implements this on the New Input System.
	/// </summary>
	public interface IMoveInput
	{
		/// <summary>One cardinal unit direction per request.</summary>
		event Action<GridVector> MoveRequested;
	}
}
