using System;
using System.Collections.Generic;
using System.Text;
using BalsamicBits.Extensions;
using Lean.Touch;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Game.Core
{
	public class GestureForwardingHandler : MonoBehaviour
	{
		internal enum Gesture
		{
			None,
			Pinch,
			Twist,
			Swipe,
			Tap,
			TwoFingerDrag
		}

		[Serializable]
		internal struct GesturePair
		{
			public Gesture a;
			public Gesture b;
		}

		[Tooltip("Pairs of different gestures that may be active at the same time. A gesture is always allowed " +
		         "alongside itself; any pair not listed here cancels the newer gesture.")]
		[SerializeField, FormerlySerializedAs("allowedGesturePairs")]
		internal List<GesturePair> allowedSimultaneousGestures = new List<GesturePair>();

		[SerializeField, Min(1)]
		private int cancelCurrentAfterFrames = 1;

		[SerializeField] private bool logCurrentGestures;

		[SerializeField] private UnityEvent<float> pinchEvent;
		[SerializeField] private UnityEvent<float> twistEvent;
		[SerializeField] private UnityEvent<LeanFinger> swipeEvent;
		[SerializeField] private UnityEvent<LeanFinger> tapEvent;
		[SerializeField] private UnityEvent<Vector2> twoFingerDragEvent;

		private readonly Dictionary<Gesture, bool> _currentGestures = new Dictionary<Gesture, bool>();

		private readonly Dictionary<Gesture, Coroutine> _cancelGestureRoutines = new Dictionary<Gesture, Coroutine>();

		#region Unity

		private void OnEnable()
		{
			_currentGestures.Clear();
			_cancelGestureRoutines.Clear();

			foreach (Gesture gesture in EnumExtensions.GetValues<Gesture>())
			{
				_currentGestures.Add(gesture, false);
				_cancelGestureRoutines.Add(gesture, null);
			}
		}

		private void Update()
		{
			if (!logCurrentGestures)
			{
				return;
			}

			StringBuilder sb = new StringBuilder();
			foreach (Gesture gesture in EnumExtensions.GetValues<Gesture>())
			{
				if (!_currentGestures[gesture])
				{
					continue;
				}

				sb.Append($"{gesture.ToString()}, ");
			}

			if (sb.Length > 0)
			{
				Debug.Log(sb.ToString());
			}
		}

		#endregion

		#region Events

		public void OnPinch(float pinchScale)
		{
			const Gesture gesture = Gesture.Pinch;

			if (!IsGestureAllowed(gesture))
			{
				return;
			}

			pinchEvent.Invoke(pinchScale);

			FinaliseGestureCallback(gesture);
		}

		public void OnTwist(float twistDegrees)
		{
			const Gesture gesture = Gesture.Twist;

			if (!IsGestureAllowed(gesture))
			{
				return;
			}

			twistEvent.Invoke(twistDegrees);

			FinaliseGestureCallback(gesture);
		}

		public void OnSwipe(LeanFinger finger)
		{
			const Gesture gesture = Gesture.Swipe;

			if (!IsGestureAllowed(gesture))
			{
				return;
			}

			swipeEvent.Invoke(finger);

			FinaliseGestureCallback(gesture);
		}

		public void OnTap(LeanFinger finger)
		{
			const Gesture gesture = Gesture.Tap;

			if (!IsGestureAllowed(gesture))
			{
				return;
			}

			tapEvent.Invoke(finger);

			FinaliseGestureCallback(gesture);
		}

		public void OnTwoFingerDrag(Vector2 screenDelta)
		{
			const Gesture gesture = Gesture.TwoFingerDrag;

			if (!IsGestureAllowed(gesture))
			{
				return;
			}

			twoFingerDragEvent.Invoke(screenDelta);

			FinaliseGestureCallback(gesture);
		}

		#endregion

		#region Methods

		private void FinaliseGestureCallback(Gesture gesture)
		{
			_currentGestures[gesture] = true;

			SetCancellationRoutine(gesture);
		}

		private void SetCancellationRoutine(Gesture gesture)
		{
			if (_cancelGestureRoutines[gesture] != null)
			{
				StopCoroutine(_cancelGestureRoutines[gesture]);
			}

			_cancelGestureRoutines[gesture] = this.WaitForFrames(
				cancelCurrentAfterFrames,
				() =>
				{
					_currentGestures[gesture] = false;
					_cancelGestureRoutines[gesture] = null;
				});
		}

		private bool IsGestureAllowed(Gesture gestureToCheck)
		{
			foreach (KeyValuePair<Gesture, bool> current in _currentGestures)
			{
				if (current.Value && !AreAllowedTogether(allowedSimultaneousGestures, gestureToCheck, current.Key))
				{
					return false;
				}
			}

			return true;
		}

		internal static bool AreAllowedTogether(IReadOnlyList<GesturePair> allowedPairs, Gesture first, Gesture second)
		{
			if (first == second || first == Gesture.None || second == Gesture.None)
			{
				return true;
			}

			foreach (GesturePair pair in allowedPairs)
			{
				if ((pair.a == first && pair.b == second) || (pair.a == second && pair.b == first))
				{
					return true;
				}
			}

			return false;
		}

		#endregion
	}
}