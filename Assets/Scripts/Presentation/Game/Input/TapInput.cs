using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
	/// <summary>
	/// Taps on the board: the primary touch on a device, the mouse in the editor. A tap is a press and release that
	/// stays within <c>maxDistanceInches</c> and <c>maxDuration</c>, so swipes aren't taps. Touches that start on UI are
	/// ignored, as is everything from the moment a second finger lands until every finger has lifted. Poll with
	/// <see cref="Tick"/> once a frame.
	/// </summary>
	internal sealed class TapInput
	{
		/// <summary>Assumed when the platform doesn't report a DPI (the editor, some desktops).</summary>
		private const float FallbackDpi = 160;

		private readonly float _maxDistance;
		private readonly float _maxDuration;

		private bool _isTracking;
		private bool _isMultiTouch;
		private Vector2 _start;
		private float _startTime;

		public TapInput(float maxDistanceInches, float maxDuration)
		{
			float dpi = Screen.dpi > 0 ? Screen.dpi : FallbackDpi;
			_maxDistance = maxDistanceInches * dpi;
			_maxDuration = maxDuration;
		}

		/// <summary>True on the frame a tap ends, with its screen position.</summary>
		public bool Tick(out Vector2 position)
		{
			position = default;

			Pointer pointer = Pointer.current;
			if (pointer == null)
			{
				_isTracking = false;
				return false;
			}

			int touchCount = ScreenTouches.CountPressed();
			if (touchCount >= 2)
			{
				_isMultiTouch = true;
				_isTracking = false;
			}
			else if (touchCount == 0 && !pointer.press.isPressed)
			{
				_isMultiTouch = false;
			}

			if (_isMultiTouch)
			{
				return false;
			}

			Vector2 current = pointer.position.ReadValue();

			if (pointer.press.wasPressedThisFrame)
			{
				_isTracking = !ScreenTouches.IsOverCanvas(current);
				_start = current;
				_startTime = Time.unscaledTime;
				return false;
			}

			if (!_isTracking)
			{
				return false;
			}

			if ((current - _start).sqrMagnitude > _maxDistance * _maxDistance ||
			    Time.unscaledTime - _startTime > _maxDuration)
			{
				_isTracking = false;
				return false;
			}

			if (!pointer.press.wasReleasedThisFrame)
			{
				return false;
			}

			_isTracking = false;
			position = current;
			return true;
		}
	}
}
