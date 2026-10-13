using System.Collections;
using BalsamicBits.Extensions;
using Core.EventChannels;
using UnityEngine;
using Game.Core;
using Game.Mowers.Input;
using Game.Mowers.Models;
using Game.UndoSystem;

namespace Game.Mowers
{
	internal class MowerTransformAnimator : MonoBehaviour, INeedMowerPosition, INeedPositioner
	{
		[SerializeField] private AnimationSpeedHandler animationSpeedHandler;
		[SerializeField] private AnimationCurve animEase;

		[Header("Spring Jumps")]
		[SerializeField, Min(0)] private float jumpHeight = 1.5f;
		[SerializeField, Min(0.05f)] private float jumpDuration = 0.45f;

		[Header("Event Channels")]
		[SerializeField] private IVector3EventChannelTransmitterContainer movedEventChannel;
		[SerializeField] private IBoolEventChannelListenerContainer isMowerTransformControlledExternallyEventChannelContainer;

		private IVector3EventChannelTransmitter MovedEventChannel => movedEventChannel.Result;
		private IBoolEventChannelListener IsMowerTransformControlledExternallyEventChannel => isMowerTransformControlledExternallyEventChannelContainer.Result;

		private Positioner _positioner;
		private IMowerPosition _mowerPosition;

		private Coroutine _translationRoutine;
		private Coroutine _rotationRoutine;
		private Vector3? _pendingJumpTarget;
		
		private bool _transformIsControlledExternally;

		#region Unity

		private void OnEnable()
		{
			IsMowerTransformControlledExternallyEventChannel.EventRaised += OnIsMowerTransformControlledExternally;
		}

		private void OnDisable()
		{
			IsMowerTransformControlledExternallyEventChannel.EventRaised -= OnIsMowerTransformControlledExternally;
		}

		#endregion

		#region API

		void INeedPositioner.Set(Positioner positioner)
		{
			_positioner = positioner;
		}

		void INeedMowerPosition.Set(IMowerPosition mowerPosition)
		{
			_mowerPosition = mowerPosition;
			_mowerPosition.CurrentPosition.ValueChangedFromTo += OnMowerMoved;
		}

		void INeedMowerPosition.Clear()
		{
			_mowerPosition.CurrentPosition.ValueChangedFromTo -= OnMowerMoved;
			_mowerPosition = null;
		}

		#endregion

		#region Events

		private void OnIsMowerTransformControlledExternally(bool isControlledExternally)
		{
			_transformIsControlledExternally = isControlledExternally;
		}

		private void OnMowerMoved(GridVector prevPosition, GridVector targetPosition, Xor inInverted)
		{
			if (_transformIsControlledExternally)
			{
				Debug.Log("Transform is being controlled elsewhere");
				return;
			}

			if (!isActiveAndEnabled)
			{
				// Hidden (before the drop-in): coroutines can't run, so just keep the position current.
				_pendingJumpTarget = null;
				SnapPosition(targetPosition);
			}
			else if ((targetPosition - prevPosition).IsCardinalUnit)
			{
				_pendingJumpTarget = null;
				AnimateRotation(prevPosition, targetPosition, inInverted);
				AnimationPosition(targetPosition, inInverted);
			}
			else if (!inInverted)
			{
				// A spring jump (B3): arc to the landing tile once the step onto the spring has finished.
				Vector3 landing = _positioner.GetWorldPosition(targetPosition);

				if (_translationRoutine != null)
				{
					_pendingJumpTarget = landing;
				}
				else
				{
					_translationRoutine = JumpRoutine(landing).Start(this);
				}
			}
			else
			{
				// Start placement (Zero -> start) and undoing a jump: snap. This also raises the
				// mower-moved channel that MowerCreatedAnimator waits on before showing the mower.
				_pendingJumpTarget = null;
				SnapPosition(targetPosition);
			}
		}

		private void AnimateRotation(GridVector prevPosition, GridVector targetPosition, Xor isInverted)
		{
			GridVector directionToLookGrid = targetPosition - prevPosition;
			directionToLookGrid *= isInverted ? -1 : 1;

			Vector3 directionToLook = new Vector3(directionToLookGrid.x, 0, directionToLookGrid.y);
			Quaternion lookRotation = Quaternion.LookRotation(directionToLook, Vector3.up);

			_rotationRoutine.Stop(this);
			_rotationRoutine = null;

			if (!isInverted)
			{
				_rotationRoutine = RotateRoutine(lookRotation).Start(this);
			}
			else
			{
				transform.rotation = lookRotation;
			}
		}

		private void AnimationPosition(GridVector position, Xor isInverted)
		{
			Vector3 worldPosition = _positioner.GetWorldPosition(position);

			_translationRoutine.Stop(this);
			_translationRoutine = null;

			if (!isInverted)
			{
				_translationRoutine = TranslateRoutine(worldPosition).Start(this);
			}
			else
			{
				transform.position = worldPosition;
				MovedEventChannel.Raise(worldPosition);
			}
		}

		private void SnapPosition(GridVector position)
		{
			_translationRoutine.Stop(this);
			_translationRoutine = null;

			Vector3 worldPosition = _positioner.GetWorldPosition(position);
			transform.position = worldPosition;
			MovedEventChannel.Raise(worldPosition);
		}

		private IEnumerator TranslateRoutine(Vector3 to)
		{
			void SetPosition(Vector3 pos)
			{
				transform.position = pos;
				MovedEventChannel.Raise(pos);
			}

			Vector3 from = transform.position;

			for (float time = Time.deltaTime;
				time < animationSpeedHandler.RecommendedAnimDuration;
				time += Time.deltaTime)
			{
				float interpolant = time / animationSpeedHandler.RecommendedAnimDuration;
				float easedT = animEase.Evaluate(interpolant);
				Vector3 position = Vector3.Lerp(from, to, easedT);

				SetPosition(position);

				yield return null;
			}

			SetPosition(to);

			_translationRoutine = null;

			if (_pendingJumpTarget.HasValue)
			{
				Vector3 landing = _pendingJumpTarget.Value;
				_pendingJumpTarget = null;
				_translationRoutine = JumpRoutine(landing).Start(this);
			}
		}

		private IEnumerator JumpRoutine(Vector3 to)
		{
			Vector3 from = transform.position;

			for (float time = Time.deltaTime; time < jumpDuration; time += Time.deltaTime)
			{
				float t = time / jumpDuration;
				Vector3 position = Vector3.Lerp(from, to, t) + Vector3.up * (jumpHeight * 4 * t * (1 - t));

				transform.position = position;
				MovedEventChannel.Raise(position);

				yield return null;
			}

			transform.position = to;
			MovedEventChannel.Raise(to);

			_translationRoutine = null;
		}

		private IEnumerator RotateRoutine(Quaternion to)
		{
			Quaternion from = transform.rotation;

			for (float time = Time.deltaTime;
				time < animationSpeedHandler.RecommendedAnimDuration;
				time += Time.deltaTime)
			{
				float interpolant = time / animationSpeedHandler.RecommendedAnimDuration;
				float easedT = animEase.Evaluate(interpolant);
				transform.rotation = Quaternion.Slerp(from, to, easedT);

				yield return null;
			}

			transform.rotation = to;

			_rotationRoutine = null;
		}

		#endregion
	}
}