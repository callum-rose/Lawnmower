using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Lean.Touch
{
	/// <summary>This component allows you to translate the current GameObject relative to the camera using the finger drag gesture.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanDragTranslate")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Drag Translate")]
	public class LeanDragTranslate : MonoBehaviour
	{
		public enum Mode
		{
			Translate,
			TranslateUI,
			TransmitWorldDelta
		}

		/// <summary>The method used to find fingers to use with this component. See LeanFingerFilter documentation for more information.</summary>
		public LeanFingerFilter Use = new LeanFingerFilter(true);

		/// <summary>The camera the translation will be calculated using.\n\nNone = MainCamera.</summary>
		[Tooltip("The camera the translation will be calculated using.\n\nNone = MainCamera.")]
		public Camera Camera;

		/// <summary>The sensitivity of the translation.
		/// 1 = Default.
		/// 2 = Double.</summary>
		[Tooltip("The sensitivity of the translation.\n\n1 = Default.\n2 = Double.")]
		public float Sensitivity = 1.0f;

		/// <summary>If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.
		/// -1 = Instantly change.
		/// 1 = Slowly change.
		/// 10 = Quickly change.</summary>
		[Tooltip(
			"If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.\n\n-1 = Instantly change.\n\n1 = Slowly change.\n\n10 = Quickly change.")]
		public float Dampening = -1.0f;

		/// <summary>This allows you to control how much momenum is retained when the dragging fingers are all released.
		/// NOTE: This requires <b>Dampening</b> to be above 0.</summary>
		[Tooltip(
			"This allows you to control how much momenum is retained when the dragging fingers are all released.\n\nNOTE: This requires <b>Dampening</b> to be above 0.")]
		[Range(0.0f, 1.0f)]
		public float Inertia;

		[TitleGroup("Custom Stuff")]
		[SerializeField] private Mode mode;

		[SerializeField] private UnityEvent<Vector2> dragDeltaEvent;
		[SerializeField, Min(0)] private float dragStartScreenDistanceThreshold;

		[ShowInInspector, ReadOnly] private bool _isPotentiallyDragging;
		[ShowInInspector, ReadOnly] private bool _isDragging;
		[ShowInInspector, ReadOnly] private Vector2 _cumulativeScreenDrag;

		[HideInInspector]
		[SerializeField]
		private Vector3 remainingTranslation;

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually add a finger.</summary>
		public void AddFinger(LeanFinger finger)
		{
			Use.AddFinger(finger);
		}

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually remove a finger.</summary>
		public void RemoveFinger(LeanFinger finger)
		{
			Use.RemoveFinger(finger);
		}

		/// <summary>If you've set Use to ManuallyAddedFingers, then you can call this method to manually remove all fingers.</summary>
		public void RemoveAllFingers()
		{
			Use.RemoveAllFingers();
		}

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}
#endif

		protected virtual void Awake()
		{
			Use.UpdateRequiredSelectable(gameObject);
		}

		protected virtual void Update()
		{
			// Store
			Vector3 oldPosition = transform.localPosition;

			// Get the fingers we want to use
			List<LeanFinger> fingers = Use.GetFingers();

			if (fingers.IsNullOrEmpty())
			{
				_isDragging = false;
				_isPotentiallyDragging = false;
				_cumulativeScreenDrag = Vector2.zero;
			}

			// Calculate the screenDelta value based on these fingers
			Vector2 screenDelta = LeanGesture.GetScreenDelta(fingers);

			if (_isPotentiallyDragging || screenDelta != Vector2.zero)
			{
				_isPotentiallyDragging = true;
				_cumulativeScreenDrag += screenDelta;

				bool canStartDragging = _cumulativeScreenDrag.sqrMagnitude >
				                        dragStartScreenDistanceThreshold * dragStartScreenDistanceThreshold;
				if (_isDragging || canStartDragging)
				{
					if (!_isDragging)
					{
						screenDelta += _cumulativeScreenDrag - screenDelta;
						_isDragging = true;
					}

					// Perform the translation
					switch (mode)
					{
						case Mode.TranslateUI:
							TranslateUI(screenDelta);
							break;
						case Mode.Translate:
							Translate(screenDelta);
							break;
						case Mode.TransmitWorldDelta:
							TransmitDelta(screenDelta);
							break;
					}
				}
			}

			// Increment
			remainingTranslation += transform.localPosition - oldPosition;

			// Get t value
			float factor = LeanTouch.GetDampenFactor(Dampening, Time.deltaTime);

			// Dampen remainingDelta
			Vector3 newRemainingTranslation = Vector3.Lerp(remainingTranslation, Vector3.zero, factor);

			// Shift this transform by the change in delta
			transform.localPosition = oldPosition + remainingTranslation - newRemainingTranslation;

			if (fingers.Count == 0 && Inertia > 0.0f && Dampening > 0.0f)
			{
				newRemainingTranslation = Vector3.Lerp(newRemainingTranslation, remainingTranslation, Inertia);
			}

			// Update remainingDelta with the dampened value
			remainingTranslation = newRemainingTranslation;
		}

		private void TranslateUI(Vector2 screenDelta)
		{
			Camera camera = Camera;

			if (camera == null)
			{
				Canvas canvas = transform.GetComponentInParent<Canvas>();

				if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
				{
					camera = canvas.worldCamera;
				}
			}

			// Screen position of the transform
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, transform.position);

			// Add the deltaPosition
			screenPoint += screenDelta * Sensitivity;

			// Convert back to world space
			Vector3 worldPoint = default(Vector3);

			if (RectTransformUtility.ScreenPointToWorldPointInRectangle(transform.parent as RectTransform, screenPoint,
				camera, out worldPoint))
			{
				transform.position = worldPoint;
			}
		}

		private void Translate(Vector2 screenDelta)
		{
			// Make sure the camera exists
			Camera camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				// Screen position of the transform
				Vector3 screenPoint = camera.WorldToScreenPoint(transform.position);

				// Add the deltaPosition
				screenPoint += (Vector3) screenDelta * Sensitivity;

				Vector3 newPosition = camera.ScreenToWorldPoint(screenPoint);

				// Convert back to world space
				transform.position = newPosition;
			}
			else
			{
				Debug.LogError(
					"Failed to find camera. Either tag your camera as MainCamera, or set one in this component.", this);
			}
		}

		private void TransmitDelta(Vector2 screenDelta)
		{
			Vector3 sensitisedScreenDelta = (Vector3) screenDelta * Sensitivity;

			dragDeltaEvent.Invoke(sensitisedScreenDelta);
		}
	}
}