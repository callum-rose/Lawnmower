using BalsamicBits.Extensions;
using Lean.Touch;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core
{
	public class GestureForwardingHandler : MonoBehaviour
	{
		private enum Gesture
		{
			None,
			Pinch,
			Twist,
			Swipe,
			Tap
		}

		[SerializeField, Min(1)] private int cancelCurrentAfterFrames = 1;
		[SerializeField] private UnityEvent<float> pinchEvent;
		[SerializeField] private UnityEvent<float> twistEvent;
		[SerializeField] private UnityEvent<LeanFinger> swipeEvent;
		[SerializeField] private UnityEvent<LeanFinger> tapEvent;

		[ShowInInspector, ReadOnly] private Gesture _currentGesture = Gesture.None;

		private Coroutine _endCurrentRoutine;

		public void OnPinch(float pinchScale)
		{
			const Gesture gesture = Gesture.Pinch;

			if (!IsCurrentGesture(gesture))
			{
				return;
			}
			
			pinchEvent.Invoke(pinchScale);
			
			FinaliseCallback(gesture);
		}

		public void OnTwist(float twistDegrees)
		{
			const Gesture gesture = Gesture.Twist;
			
			if (!IsCurrentGesture(gesture))
			{
				return;
			}
			
			twistEvent.Invoke(twistDegrees);

			FinaliseCallback(gesture);
		}

		public void OnSwipe(LeanFinger finger)
		{
			const Gesture gesture = Gesture.Swipe;
			
			if (!IsCurrentGesture(gesture))
			{
				return;
			}
			
			swipeEvent.Invoke(finger);

			FinaliseCallback(gesture);
		}
		
		public void OnTap(LeanFinger finger)
		{
			const Gesture gesture = Gesture.Tap;
			
			if (!IsCurrentGesture(gesture))
			{
				return;
			}
			
			tapEvent.Invoke(finger);

			FinaliseCallback(gesture);
		}

		private void SetCancellationRoutine()
		{
			if (_endCurrentRoutine != null)
			{
				StopCoroutine(_endCurrentRoutine);
			}

			_endCurrentRoutine = StartCoroutine(CoroutineExtensions.WaitForFrames(
				cancelCurrentAfterFrames,
				() =>
				{
					_currentGesture = Gesture.None;
					_endCurrentRoutine = null;
				}));
		}

		private void FinaliseCallback(Gesture gesture)
		{
			_currentGesture = gesture;

			SetCancellationRoutine();
		}

		private bool IsCurrentGesture(Gesture gesture)
		{
			return _currentGesture == Gesture.None || _currentGesture == gesture;
		}
	}
}