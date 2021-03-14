using System.Collections;
using BalsamicBits.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LevelSelect
{
	internal class LevelsScrollViewManager : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		[SerializeField] private ScrollRect scrollRect;
		[SerializeField, Min(0)] private float stopSnappingSpeedThreshold;
		[SerializeField, Min(0)] private float stopSnappingPositionThreshold;
		[SerializeField] private AnimationCurve forceFactorByDistance;
		private Coroutine _magneticRoutine;

		private int LevelCount => scrollRect.content.childCount;
		private float CurrentXPosition => scrollRect.horizontalNormalizedPosition;

		private float VelocityX
		{
			get => scrollRect.velocity.x;
			set
			{
				Vector2 vel = scrollRect.velocity;
				vel.x = value;
				scrollRect.velocity = vel;
			}
		}

		private void OnEnable()
		{
			scrollRect.horizontalNormalizedPosition = 0;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			_magneticRoutine.Stop(this);
		}

		public void OnDrag(PointerEventData eventData)
		{
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			_magneticRoutine = MagneticSnapRoutine().Start(this);
		}

		private IEnumerator MagneticSnapRoutine()
		{
			while (true)
			{
				float nearestXSnap = GetNearestXPosToSnapTo();
				float currentToSnapDelta = CurrentXPosition - nearestXSnap;

				if (Mathf.Abs(currentToSnapDelta) < stopSnappingPositionThreshold && Mathf.Abs(VelocityX) < stopSnappingSpeedThreshold)
				{
					break;
				}
				
				float force = Mathf.Sign(currentToSnapDelta) * forceFactorByDistance.Evaluate(Mathf.Abs(currentToSnapDelta));

				VelocityX += force;
				VelocityX *= 1 - scrollRect.decelerationRate * Time.deltaTime;
				
				Debug.Log(VelocityX);

				yield return null;
			}

			VelocityX = 0;
			_magneticRoutine = null;
		}

		private float GetNearestXPosToSnapTo()
		{
			int nearestChildIndex = Mathf.RoundToInt(UnnormalisePosition(CurrentXPosition));
			return NormalisePosition(nearestChildIndex);
		}

		private float UnnormalisePosition(float x)
		{
			return x * (LevelCount - 1);
		}

		private float NormalisePosition(float x)
		{
			return x / (LevelCount - 1);
		}
	}
}