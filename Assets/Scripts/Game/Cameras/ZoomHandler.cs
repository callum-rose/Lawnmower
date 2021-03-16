using System;
using Cinemachine;
using UnityEngine;

namespace Game.Cameras
{
	internal class ZoomHandler : MonoBehaviour
	{
		[SerializeField] private CinemachineMixingCamera mixingCamera;
		[SerializeField, Range(0, 1)] private float speedFactor = 1;
		[SerializeField] private bool invertDirection;

		private float SecondCamAmount => 1 - _firstCamAmount;
		
		private float _firstCamAmount = 1;
		
		public void OnPinchGesture(float pinchScale)
		{			
			if (invertDirection)
			{
				pinchScale = 1 / pinchScale;
			}
			
			_firstCamAmount *= Mathf.Pow(pinchScale, speedFactor);
			_firstCamAmount = Mathf.Clamp01(_firstCamAmount);
			
			mixingCamera.SetWeight(0, _firstCamAmount);
			mixingCamera.SetWeight(1, SecondCamAmount);
		}
	}
}