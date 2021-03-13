using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Lean.Touch
{
	/// <summary>This component allows you to scale the current GameObject relative to the specified camera using the pinch gesture.</summary>
	[HelpURL(LeanTouch.HelpUrlPrefix + "LeanPinchScale")]
	[AddComponentMenu(LeanTouch.ComponentPathPrefix + "Pinch Scale")]
	public class LeanPinchScale : MonoBehaviour
	{
		/// <summary>The method used to find fingers to use with this component. See LeanFingerFilter documentation for more information.</summary>
		public LeanFingerFilter Use = new LeanFingerFilter(true);

		/// <summary>The camera that will be used to calculate the zoom.
		/// None = MainCamera.</summary>
		[Tooltip("The camera that will be used to calculate the zoom.\n\nNone = MainCamera.")]
		public Camera Camera;

		/// <summary>Should the scaling be performanced relative to the finger center?</summary>
		[Tooltip("Should the scaling be performanced relative to the finger center?")]
		public bool Relative;
		
		/// <summary>The sensitivity of the scaling.
		/// 1 = Default.
		/// 2 = Double.</summary>
		[Tooltip("The sensitivity of the scaling.\n\n1 = Default.\n2 = Double.")]
		public float Sensitivity = 1.0f;

		/// <summary>If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.
		/// -1 = Instantly change.
		/// 1 = Slowly change.
		/// 10 = Quickly change.</summary>
		[Tooltip("If you want this component to change smoothly over time, then this allows you to control how quick the changes reach their target value.\n\n-1 = Instantly change.\n\n1 = Slowly change.\n\n10 = Quickly change.")]
		public float Dampening = -1.0f;

		public UnityEvent<float> Scale; 
		
		[TitleGroup("Custom Stuff")]
		[SerializeField, Min(1)] private float pinchStartThresholdFactor;

		[ShowInInspector, ReadOnly] private float PinchStartThresholdFactorUpper => 1 * pinchStartThresholdFactor;
		[ShowInInspector, ReadOnly] private float PinchStartThresholdFactorLower => 1 / pinchStartThresholdFactor;

		[ShowInInspector, ReadOnly]
		private bool _isPotentiallyPinching;

		[ShowInInspector, ReadOnly]
		private bool _isPinching;

		[ShowInInspector, ReadOnly]
		private float _cumulativePinchScale = 1;

		[HideInInspector]
		[SerializeField]
		private Vector3 remainingScale;

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
			Vector3 oldScale = transform.localPosition;

			// Get the fingers we want to use
			List<LeanFinger> fingers = Use.GetFingers();

			if (fingers.IsNullOrEmpty())
			{
				_isPinching = false;
				_isPotentiallyPinching = false;
				_cumulativePinchScale = 1;
			}
			
			// Calculate pinch scale, and make sure it's valid
			float pinchScale = LeanGesture.GetPinchScale(fingers);
			
			if (_isPotentiallyPinching || Mathf.Abs(pinchScale - 1.0f) > Mathf.Epsilon)
			{
				_isPotentiallyPinching = true;
				
				pinchScale = Mathf.Pow(pinchScale, Sensitivity);

				_cumulativePinchScale *= pinchScale;

				bool SurpassedThreshold() => _cumulativePinchScale >= PinchStartThresholdFactorUpper || _cumulativePinchScale <= PinchStartThresholdFactorLower;
				if (_isPinching || SurpassedThreshold())
				{
					if (!_isPinching)
					{
						pinchScale *= _cumulativePinchScale / pinchScale;
						_isPinching = true;
					}
					
					// Perform the translation if this is a relative scale
					if (Relative)
					{
						Vector2 pinchScreenCenter = LeanGesture.GetScreenCenter(fingers);

						if (transform is RectTransform)
						{
							TranslateUI(pinchScale, pinchScreenCenter);
						}
						else
						{
							Translate(pinchScale, pinchScreenCenter);
						}
					}

					transform.localScale *= pinchScale;

					if (Scale != null)
					{
						Scale.Invoke(pinchScale);
					}

					remainingScale += transform.localPosition - oldScale;
				}
			}

			// Get t value
			float factor = LeanTouch.GetDampenFactor(Dampening, Time.deltaTime);

			// Dampen remainingDelta
			Vector3 newRemainingScale = Vector3.Lerp(remainingScale, Vector3.zero, factor);

			// Shift this transform by the change in delta
			transform.localPosition = oldScale + remainingScale - newRemainingScale;

			// Update remainingDelta with the dampened value
			remainingScale = newRemainingScale;
		}

		protected virtual void TranslateUI(float pinchScale, Vector2 pinchScreenCenter)
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

			// Push the screen position away from the reference point based on the scale
			screenPoint.x = pinchScreenCenter.x + (screenPoint.x - pinchScreenCenter.x) * pinchScale;
			screenPoint.y = pinchScreenCenter.y + (screenPoint.y - pinchScreenCenter.y) * pinchScale;

			// Convert back to world space
			Vector3 worldPoint = default(Vector3);

			if (RectTransformUtility.ScreenPointToWorldPointInRectangle(transform.parent as RectTransform, screenPoint, camera, out worldPoint))
			{
				transform.position = worldPoint;
			}
		}

		protected virtual void Translate(float pinchScale, Vector2 screenCenter)
		{
			// Make sure the camera exists
			Camera camera = LeanTouch.GetCamera(Camera, gameObject);

			if (camera != null)
			{
				// Screen position of the transform
				Vector3 screenPosition = camera.WorldToScreenPoint(transform.position);

				// Push the screen position away from the reference point based on the scale
				screenPosition.x = screenCenter.x + (screenPosition.x - screenCenter.x) * pinchScale;
				screenPosition.y = screenCenter.y + (screenPosition.y - screenCenter.y) * pinchScale;

				// Convert back to world space
				transform.position = camera.ScreenToWorldPoint(screenPosition);
			}
			else
			{
				Debug.LogError("Failed to find camera. Either tag your cameras MainCamera, or set one in this component.", this);
			}
		}
	}
}