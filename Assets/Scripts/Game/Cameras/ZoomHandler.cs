using System;
using Cinemachine;
using UnityEngine;

namespace Game.Cameras
{
	internal class ZoomHandler : MonoBehaviour
	{
		[Serializable]
		private struct CameraZoomData
		{
			[SerializeField] private CinemachineVirtualCamera camera;
			[SerializeField, Min(0)] private float minFov;
			[SerializeField, Min(0)] private float maxFov;

			public CinemachineVirtualCamera Camera => camera;
			public float MinFov => minFov;
			public float MaxFov => maxFov;
		}

		[SerializeField] private CameraZoomData[] cameraZoomDatas;
		[SerializeField, Range(0, 1)] private float speedFactor = 1;
		[SerializeField] private bool invertDirection;
		
		private void OnEnable()
		{
			foreach (CameraZoomData cameraZoomData in cameraZoomDatas)
			{
				cameraZoomData.Camera.m_Lens.FieldOfView = cameraZoomData.MinFov;
			}
		}

		public void OnPinchGesture(float pinchScale)
		{
			foreach (CameraZoomData cameraZoomData in cameraZoomDatas)
			{
				PinchCamera(cameraZoomData, pinchScale);
			}
		}

		private void PinchCamera(CameraZoomData zoomData, float pinchScale)
		{
			float fov = zoomData.Camera.m_Lens.FieldOfView;

			if (invertDirection)
			{
				pinchScale = 1 / pinchScale;
			}

			fov *= Mathf.Pow(pinchScale, speedFactor);
			fov = Mathf.Clamp(fov, zoomData.MinFov, zoomData.MaxFov);
			zoomData.Camera.m_Lens.FieldOfView = fov;
		}
	}
}