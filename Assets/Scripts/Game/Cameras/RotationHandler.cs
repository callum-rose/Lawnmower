using System;
using System.Collections;
using BalsamicBits.Extensions;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	internal class RotationHandler : MonoBehaviour
	{
		[SerializeField] private Transform dummyMower;
		[SerializeField] private Transform targetGroupTransform;
		[SerializeField] private float maxSnapRotationAnimDuration = 1;
		[SerializeField] private int framesToWaitBeforeSnapping = 2;

		private Coroutine _waitForStartSnapRoutine;
		private Tween _dummyMowerRotationTween;
		private Tween _targetGroupRotationTween;

		public void OnTwistGesture(float twistDegrees)
		{
			dummyMower.rotation *= Quaternion.AngleAxis(twistDegrees, Vector3.up);
			targetGroupTransform.rotation *= Quaternion.AngleAxis(twistDegrees, Vector3.up);

			Debug.Log(dummyMower.rotation.eulerAngles.y + " ---- " + twistDegrees);

			_waitForStartSnapRoutine.Stop(this);
			_waitForStartSnapRoutine = this.WaitForFrames(framesToWaitBeforeSnapping, StartSnapRotation);
		}

		private void StartSnapRotation()
		{
			_dummyMowerRotationTween?.Kill();
			_dummyMowerRotationTween =
				DoSnapRotationAnimation(dummyMower).OnComplete(() => _dummyMowerRotationTween = null);

			_targetGroupRotationTween?.Kill();
			_targetGroupRotationTween = DoSnapRotationAnimation(targetGroupTransform)
				.OnComplete(() => _targetGroupRotationTween = null);
		}

		private Tween DoSnapRotationAnimation(Transform transform)
		{
			Quaternion transformRotation = transform.rotation;

			float currentRotationY = transformRotation.eulerAngles.y;
			float nearest90 = Mathf.Round(currentRotationY / 90) * 90;
			float delta = currentRotationY - nearest90;

			return transform.DORotate(
				transformRotation.eulerAngles.SetY(nearest90),
				maxSnapRotationAnimDuration * delta / 45,
				RotateMode.FastBeyond360);
		}
	}
}