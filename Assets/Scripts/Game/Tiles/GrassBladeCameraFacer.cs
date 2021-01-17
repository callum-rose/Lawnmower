using System;
using UnityEngine;

namespace Game.Tiles
{
	public class GrassBladeCameraFacer : MonoBehaviour
	{
		private Camera _camera;

		private Func<Vector3> _getLookDirectionFunc;

		private void Awake()
		{
			_camera = Camera.main;

			if (_camera == null)
			{
				throw new NullReferenceException("Camera main is null");
			}

			if (_camera.orthographic)
			{
				_getLookDirectionFunc = GetLookDirectionOrtho;
			}
			else
			{
				_getLookDirectionFunc = GetLookDirectionPersp;
			}
		}

		private void LateUpdate()
		{
			Quaternion rotation = Quaternion.Euler(0, -90, 0) *
			                               Quaternion.LookRotation(_getLookDirectionFunc(), Vector3.up);

			if (Quaternion.Angle(rotation, transform.rotation) < 0.1f)
			{
				return;
			}
			
			transform.rotation = rotation;
		}

		private Vector3 GetLookDirectionOrtho()
		{
			return _camera.transform.forward;
		}

		private Vector3 GetLookDirectionPersp()
		{
			return transform.position - _camera.transform.position;
		}
	}
}