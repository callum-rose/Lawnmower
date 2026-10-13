using System.Collections.Generic;
using BalsamicBits.Extensions;
using DG.Tweening;
using UnityEngine;

namespace Game.Cameras
{
	internal class RotationHandler : MonoBehaviour
	{
		[SerializeField] private Transform[] transforms;
		[SerializeField] private float maxSnapRotationAnimDuration = 1;
		[SerializeField] private int framesToWaitBeforeSnapping = 2;

		private Coroutine _waitForStartSnapRoutine;
		private Dictionary<Transform, Tween> _transformTweens = new Dictionary<Transform, Tween>();

		private void OnEnable()
		{
			_transformTweens.Clear();
			foreach (Transform transform in transforms)
			{
				_transformTweens.Add(transform, null);
			}
		}

		public void OnTwistGesture(float twistDegrees)
		{
			Quaternion angleAxis = Quaternion.AngleAxis(twistDegrees, Vector3.up);
			foreach (Transform transform in transforms)
			{
				transform.rotation *= angleAxis;
			}
			
			_waitForStartSnapRoutine.Stop(this);
			_waitForStartSnapRoutine = this.WaitForFrames(framesToWaitBeforeSnapping, StartSnapRotation);
		}

		private void StartSnapRotation()
		{
			foreach (Transform transform in transforms)
			{
				Tween tween = _transformTweens[transform];
				tween?.Kill();
				
				_transformTweens[transform] = DoSnapRotationAnimation(transform)
					.OnComplete(() => _transformTweens[transform] = null);
			}
		}

		private Tween DoSnapRotationAnimation(Transform transform)
		{
			Quaternion transformRotation = transform.rotation;

			float currentRotationY = transformRotation.eulerAngles.y;
			float nearest90 = Mathf.Round(currentRotationY / 90) * 90;
			float delta = currentRotationY - nearest90;
			float duration = Mathf.Abs(maxSnapRotationAnimDuration * delta / 45);
			
			return transform.DORotate(
				transformRotation.eulerAngles.SetY(nearest90),
				duration,
				RotateMode.FastBeyond360);
		}
	}
}