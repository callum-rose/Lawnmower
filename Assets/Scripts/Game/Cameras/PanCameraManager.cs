using System.Collections;
using BalsamicBits.Extensions;
using Core.EventChannels;
using Game.Core;
using Game.Levels;
using Game.Mowers.Input;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	internal class PanCameraManager : MonoBehaviour
	{
		[SerializeField] private Transform panTarget;
		[SerializeField] private Transform mowerTarget;
		[SerializeField] private MixCameraAnimator mixCameraAnimator;
		[SerializeField] private int mowerCamMixIndex = 0;
		[SerializeField] private int dragCamMixIndex = 1;
		[SerializeField, Min(0)] private float snapToMowerThresholdTileWidths = 1;
		[SerializeField, Min(0)] private float snapAnimDuration = 0.5f;

		[TitleGroup("Event Channels")]
		[SerializeField] private IMowerInputEventChannelListenerContainer mowerInputEventChannel;

		[SerializeField] private IBoolEventChannelListenerContainer canPanCameraEventChannel;

		private IMowerInputEventChannelListener MowerInputEventChannel => mowerInputEventChannel.Result;
		private IBoolEventChannelListener CanPanCameraEventChannel => canPanCameraEventChannel.Result;

		private bool _isDragging;
		private bool _isSnappedToMower;
		private Coroutine _setIsNotDraggingRoutine;
		private float _snappedAmount;
		private Coroutine _snapAmountRoutine;

		private void OnEnable()
		{
			MowerInputEventChannel.EventRaised += MowerInputEventChannelOnEventRaised;
			CanPanCameraEventChannel.EventRaised += OnCanPanCameraSet;
		}

		private void Start()
		{
			SetSnapped(true);
		}

		private void OnDisable()
		{
			MowerInputEventChannel.EventRaised -= MowerInputEventChannelOnEventRaised;
			CanPanCameraEventChannel.EventRaised -= OnCanPanCameraSet;
		}

		public void OnDragGesture(Vector2 screenDelta)
		{
			_isDragging = true;

			_setIsNotDraggingRoutine.Stop(this);
			_setIsNotDraggingRoutine = this.WaitForFrames(1, () => _isDragging = false);
		}

		private void LateUpdate()
		{
			if (_isDragging)
			{
				SetSnapped(false);
				return;
			}

			if (!_isSnappedToMower)
			{
				float positionDistanceFromDummyMowerSqr = PanTargetSqrDistanceFrom(mowerTarget.position);
				float worldDistanceThreshold = snapToMowerThresholdTileWidths * LevelDimensions.TileSize;
				if (positionDistanceFromDummyMowerSqr <= worldDistanceThreshold * worldDistanceThreshold)
				{
					SetSnapped(true);
				}
			}

			panTarget.position = Vector3.Lerp(panTarget.position, mowerTarget.position, _snappedAmount);
		}

		private void MowerInputEventChannelOnEventRaised(GridVector _)
		{
			SetSnapped(true);
		}

		private void OnCanPanCameraSet(bool canPan)
		{
			if (canPan)
			{
				SetSnapped(true);
			}
		}

		private void SetSnapped(bool isSnapped)
		{
			if (_isSnappedToMower == isSnapped)
			{
				return;
			}

			_isSnappedToMower = isSnapped;

			mixCameraAnimator.AnimateTo(_isSnappedToMower ? mowerCamMixIndex : dragCamMixIndex);

			_snapAmountRoutine.Stop(this);
			_snapAmountRoutine = AnimateSnappedAmount(_isSnappedToMower ? 1 : 0).Start(this);
		}

		private IEnumerator AnimateSnappedAmount(float target)
		{
			float initial = _snappedAmount;

			for (float t = Time.deltaTime; t < snapAnimDuration; t += Time.deltaTime)
			{
				float normalisedTime = t / snapAnimDuration;
				_snappedAmount = Mathf.Lerp(initial, target, normalisedTime);

				yield return null;
			}

			_snappedAmount = target;
		}

		private float PanTargetSqrDistanceFrom(Vector3 position) => Vector3.SqrMagnitude(panTarget.position - position);
	}
}