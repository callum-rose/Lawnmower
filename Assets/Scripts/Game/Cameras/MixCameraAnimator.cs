using System;
using System.Collections;
using BalsamicBits.Extensions;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	[RequireComponent(typeof(CinemachineMixingCamera))]
	internal class MixCameraAnimator : MonoBehaviour
	{
		[SerializeField] private float duration;
		[SerializeField] private AnimationCurve mixCurve;

		private CinemachineMixingCamera _mixingCamera;

		private int _lastIndexSet;
		
		private void Awake()
		{
			_mixingCamera = GetComponent<CinemachineMixingCamera>();
		}

		[Button("Debug Animate To")]
		public void AnimateTo(int index)
		{
			if (index < 0 || index >= _mixingCamera.ChildCameras.Length)
			{
				Debug.LogError("Index out of range");
				return;
			}
			
			StopAllCoroutines();

			for (int i = 0; i < _mixingCamera.ChildCameras.Length; i++)
			{
				if (i == index)
				{
					AnimateWeightAtIndex(i, 1).Start(this);
				}
				else
				{
					AnimateWeightAtIndex(i, 0).Start(this);
				}
			}

			_lastIndexSet = index;
		}
		
		[Button("Debug Animate Toggle")]
		public void AnimateToggle()
		{
			AnimateTo((_lastIndexSet + 1) % _mixingCamera.ChildCameras.Length);
		}

		private IEnumerator AnimateWeightAtIndex(int index, float target)
		{
			float initialWeight = _mixingCamera.GetWeight(index);

			for (float t = 0; t < duration; t += Time.deltaTime)
			{
				float normalisedTime = t / duration;
				float interpolent = mixCurve.Evaluate(normalisedTime);
				float weight = Mathf.Lerp(initialWeight, target, interpolent);
				_mixingCamera.SetWeight(index, weight);

				yield return null;
			}

			_mixingCamera.SetWeight(index, target);
		}
	}
}