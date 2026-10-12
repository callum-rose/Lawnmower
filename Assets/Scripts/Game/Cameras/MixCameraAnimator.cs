using System.Collections;
using BalsamicBits.Extensions;

using UnityEngine;

namespace Game.Cameras
{
	[RequireComponent(typeof(Unity.Cinemachine.CinemachineMixingCamera))]
	internal class MixCameraAnimator : MonoBehaviour
	{
		[SerializeField] private float duration;
		[SerializeField] private AnimationCurve mixCurve;
		[SerializeField] private int initialIndex = 0;

		private Unity.Cinemachine.CinemachineMixingCamera _mixingCamera;

		private int _lastIndexSet;
		
		private void Awake()
		{
			_mixingCamera = GetComponent<Unity.Cinemachine.CinemachineMixingCamera>();
			
			for (int i = 0; i < _mixingCamera.ChildCameras.Count; i++)
			{
				float weight = i == initialIndex ? 1 : 0;
				_mixingCamera.SetWeight(i, weight);
			}
		}

		public void AnimateTo(int index)
		{
			if (index < 0 || index >= _mixingCamera.ChildCameras.Count)
			{
				Debug.LogError("Index out of range");
				return;
			}
			
			StopAllCoroutines();

			for (int i = 0; i < _mixingCamera.ChildCameras.Count; i++)
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
		
		[ContextMenu("Debug Animate Toggle")]
		public void AnimateToggle()
		{
			AnimateTo((_lastIndexSet + 1) % _mixingCamera.ChildCameras.Count);
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