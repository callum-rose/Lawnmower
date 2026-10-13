using System;

namespace Lawnmower.Presenters
{
	/// <summary>
	/// The movement scheme the player chose. Saved settings set <see cref="Current"/> at startup and when the player
	/// changes it; the Game scene's input follows it live through <see cref="Changed"/>.
	/// </summary>
	public static class MovementSchemeSetting
	{
		public const MovementScheme Default = MovementScheme.Swipe;

		private static MovementScheme _current = Default;

		/// <summary>Raised with the new scheme when <see cref="Current"/> changes.</summary>
		public static event Action<MovementScheme> Changed;

		public static MovementScheme Current
		{
			get => _current;
			set
			{
				if (_current == value)
				{
					return;
				}

				_current = value;
				Changed?.Invoke(value);
			}
		}
	}
}
