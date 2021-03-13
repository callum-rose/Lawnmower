using System;
using BalsamicBits.Extensions;
using Core;
using DG.Tweening;
using Game.Core;
using Game.Mowers.Input;
using UnityEngine;
using UnityLayers = Core.UnityLayers;

namespace Game.UI
{
	internal class UiArrowsManager : MonoBehaviour
	{
		[SerializeField] private Camera camera;
		[SerializeField] private MowerInputEventChannel mowerInputEventChannel;
		[SerializeField] private SerialisedDictionary<GameObject, GridVector> arrowToDirectionDict;

		private const float AnimDuration = 0.2f;

		private Vector3? _lastCameraPosition;
		private Coroutine _turnOffCameraRoutine;

		private void OnEnable()
		{
			camera.enabled = true;
		}

		public void Input(Vector2 normalisedPosition)
		{
			Ray ray = camera.ViewportPointToRay(normalisedPosition);
			if (!Physics.Raycast(ray, out RaycastHit hitInfo, 1 << (int) UnityLayers.UI))
			{
				return;
			}

			camera.enabled = true;

			hitInfo.transform.DOComplete();
			hitInfo.transform.DOPunchScale(Vector3.one * -0.2f, AnimDuration, 1);

			GridVector direction = arrowToDirectionDict[hitInfo.transform.gameObject];
			mowerInputEventChannel.Raise(direction);

			_turnOffCameraRoutine.Stop(this);
			_turnOffCameraRoutine = this.Timer(AnimDuration, () =>
			{
				camera.enabled = false;
				_turnOffCameraRoutine = null;
			});
		}

		private void LateUpdate()
		{
			Vector3 currentCameraPosition = camera.transform.position;
			const float threshold = 0.01f;
			float sqrMagnitude = Vector3.SqrMagnitude(currentCameraPosition - _lastCameraPosition ?? Vector3.zero);
			bool cameraIsMoving = sqrMagnitude > threshold * threshold;

			if (cameraIsMoving)
			{
				_turnOffCameraRoutine.Stop(this);
				_turnOffCameraRoutine = null;

				camera.enabled = true;
			}
			else if (_turnOffCameraRoutine == null)
			{
				camera.enabled = false;
			}

			_lastCameraPosition = currentCameraPosition;
		}
	}
}