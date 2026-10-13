using System;
using Game.Mowers.Input;
using Lawnmower.Presenters;

namespace Game.Core
{
	/// <summary>
	/// Moves raised on the <see cref="MowerInputEventChannel"/> (keyboard, Lean Touch swipes, UI arrows).
	/// Step 6 replaces this with New Input System sources.
	/// </summary>
	internal sealed class MowerInputChannelSource : IMoveInput, IDisposable
	{
		public event Action<GridVector> MoveRequested;

		private readonly MowerInputEventChannel _channel;

		public MowerInputChannelSource(MowerInputEventChannel channel)
		{
			_channel = channel;
			_channel.EventRaised += OnInput;
		}

		public void Dispose()
		{
			_channel.EventRaised -= OnInput;
		}

		private void OnInput(GridVector direction) => MoveRequested?.Invoke(direction);
	}
}
