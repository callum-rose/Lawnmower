using System;
using System.Collections.Generic;
using System.Text;
using BalsamicBits.Extensions;
using Lean.Touch;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core
{
	public class GestureForwardingHandler : SerializedMonoBehaviour
	{
		private enum Gesture
		{
			None,
			Pinch,
			Twist,
			Swipe,
			Tap,
			TwoFingerDrag
		}

		[BoxGroup(GesturesBoxGroup), OdinSerialize,
		 TableMatrix(SquareCells = true, DrawElementMethod = nameof(DrawTableCell), RowHeight = 20,
			 ResizableColumns = false),
		 InfoBox("$" + nameof(MatrixLegend))]
		private bool[,] allowedSimultaneousGestures;

		[SerializeField, Min(1)]
		private int cancelCurrentAfterFrames = 1;

		[SerializeField] private bool logCurrentGestures;

		[SerializeField] private UnityEvent<float> pinchEvent;
		[SerializeField] private UnityEvent<float> twistEvent;
		[SerializeField] private UnityEvent<LeanFinger> swipeEvent;
		[SerializeField] private UnityEvent<LeanFinger> tapEvent;
		[SerializeField] private UnityEvent<Vector2> twoFingerDragEvent;

		[BoxGroup(GesturesBoxGroup), ShowInInspector, ReadOnly, HideInEditorMode]
		private readonly Dictionary<Gesture, bool> _currentGestures = new Dictionary<Gesture, bool>();

		private readonly Dictionary<Gesture, Coroutine> _cancelGestureRoutines = new Dictionary<Gesture, Coroutine>();

		private const string GesturesBoxGroup = "Gestures";

		private string MatrixLegend
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				foreach (Gesture gesture in EnumExtensions.GetValues<Gesture>())
				{
					sb.Append($"{gesture.ToString()} = {(int) gesture}, ");
				}

				return sb.ToString();
			}
		}

		private int GestureCount => Enum.GetNames(typeof(Gesture)).Length;

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
			for (int i = 0; i < GestureCount; i++)
			{
				Gesture gesture = (Gesture) i;

				bool gestureIsActive = _currentGestures[gesture];
				if (!gestureIsActive)
				{
					continue;
				}

				int x = (int) gestureToCheck;
				int y = i;

				bool gesturesAreAllowedTogether =
					x > y ? allowedSimultaneousGestures[y, x] : allowedSimultaneousGestures[x, y];

				if (!gesturesAreAllowedTogether)
				{
					return false;
				}
			}

			return true;
		}

		[BoxGroup(GesturesBoxGroup), Button]
		private void BuildMatrix()
		{
			allowedSimultaneousGestures = new bool[GestureCount, GestureCount];

			for (int i = 0; i < GestureCount; i++)
			{
				allowedSimultaneousGestures[i, i] = true;
				allowedSimultaneousGestures[0, i] = true;
			}
		}

		private static bool DrawTableCell(Rect rect, bool[,] matrix, int x, int y)
		{
#if UNITY_EDITOR
			if (x > y)
			{
				return false;
			}

			bool disable = x == y || x == 0;
			if (disable)
			{
				GUI.enabled = false;
			}

			bool value = matrix[x, y];
			bool result = EditorGUI.Toggle(rect, value);

			if (disable)
			{
				GUI.enabled = true;
			}

			return result;

#else
			return matrix[x,y];
#endif
		}

		#endregion
	}
}