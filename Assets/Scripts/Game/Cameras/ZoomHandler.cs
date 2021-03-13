using System;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	internal class ZoomHandler : MonoBehaviour
	{
		[SerializeField] private CinemachineVirtualCamera camera;
		[SerializeField] private float minFov = 7;
		[SerializeField] private float maxFov = 10;
		[SerializeField, Range(0, 1)] private float speedFactor = 1;
		[SerializeField] private bool invertDirection;
		
		private void OnEnable()
		{
			camera.m_Lens.FieldOfView = minFov;
		}

		public void OnPinchGesture(float pinchScale)
		{
			float fov = camera.m_Lens.FieldOfView;
			
			if (invertDirection)
			{
				pinchScale = 1 / pinchScale;
			}

			fov *= Mathf.Pow(pinchScale, speedFactor);
			fov = Mathf.Clamp(fov, minFov, maxFov);
			camera.m_Lens.FieldOfView = fov;
		}
	}
}