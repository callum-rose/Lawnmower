using BalsamicBits.Extensions;
using Core.EventChannels;
using Game.Core;
using Game.Levels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	internal class TwoFingerDragHandler : MonoBehaviour
	{
		[SerializeField] private ScreenToWorldPointConverter screenToWorldConverter;
		[SerializeField] private Transform transformToDrag;
		[SerializeField] private bool invertDirection;

		[SerializeField] private Transform dummyMower;
		[SerializeField] private Positioner positioner;
		[SerializeField, Min(0)] private float tileWidthsRadialOffset = 1;
		[SerializeField, Min(0)] private float outsideBoundaryForceMultiplier = 0.1f;

		[TitleGroup("Event Channels")]
		[SerializeField] private ILevelDataEventChannelListenerContainer levelDataEventChannel;
		[SerializeField] private IBoolEventChannelListenerContainer canPanCameraEventChannel;
		
		private ILevelDataEventChannelListener LevelDataEventChannelListener => levelDataEventChannel.Result;
		private IBoolEventChannelListener CanPanCameraEventChannel => canPanCameraEventChannel.Result;

		private float BoundaryRadius => (_levelRadius ?? 0) + tileWidthsRadialOffset * LevelDimensions.TileSize;

		private bool _canPan = true;
		
		private Vector3? _levelCenter;
		private float? _levelRadius;
		
		#region Unity

		private void OnEnable()
		{
			LevelDataEventChannelListener.EventRaised += OnLevelData;
			CanPanCameraEventChannel.EventRaised += OnCanPanCamera;
		}

		private void OnDisable()
		{
			LevelDataEventChannelListener.EventRaised -= OnLevelData;
			CanPanCameraEventChannel.EventRaised -= OnCanPanCamera;
		}

		private void OnDrawGizmosSelected()
		{
			if (!_levelCenter.HasValue || !_levelRadius.HasValue)
			{
				return;
			}

			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(_levelCenter.Value, _levelRadius.Value);
			Gizmos.color = Color.black;
			Gizmos.DrawWireSphere(_levelCenter.Value, BoundaryRadius);
		}

		private void LateUpdate()
		{
			if (!_levelCenter.HasValue || !_levelRadius.HasValue)
			{
				return;
			}

			transformToDrag.position = transformToDrag.position.SetY(dummyMower.position.y);

			float positionDistFromLevelCenterSqr = TransformToDragSqrDistanceFrom(_levelCenter.Value);
			if (positionDistFromLevelCenterSqr > BoundaryRadius * BoundaryRadius)
			{
				float distOutsideBoundary = Mathf.Sqrt(positionDistFromLevelCenterSqr) - BoundaryRadius;
				float forceSize = distOutsideBoundary * outsideBoundaryForceMultiplier;
				Vector3 force = (_levelCenter.Value - transformToDrag.position).normalized * forceSize;
				transformToDrag.position += force;
			}
		}

		#endregion

		#region Events

		public void OnDragGesture(Vector2 screenDelta)
		{
			if (!_canPan)
			{
				return;
			}
			
			Vector3 startPoint = screenToWorldConverter.GetWorldPoint(Vector2.zero);
			Vector3 endPoint = screenToWorldConverter.GetWorldPoint(screenDelta);
			Vector3 worldDelta = endPoint - startPoint;

			if (invertDirection)
			{
				worldDelta *= -1;
			}

			transformToDrag.position += worldDelta.SetY(0);
		}

		private void OnLevelData(IReadOnlyLevelData levelData)
		{
			_levelCenter = positioner.GetLevelWorldCenter(levelData);
			_levelRadius = positioner.GetMaxDistanceFromLevelCenter(levelData, _levelCenter.Value);
		}
		
		
		private void OnCanPanCamera(bool canPan)
		{
			_canPan = canPan;
		}

		#endregion

		#region Methods
		
		private float TransformToDragSqrDistanceFrom(Vector3 position) => Vector3.SqrMagnitude(transformToDrag.position - position);

		#endregion
	}
}