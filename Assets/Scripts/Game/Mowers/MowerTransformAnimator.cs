using System;
using System.Collections;
using BalsamicBits.Extensions;
using Core;
using Core.EventChannels;
using UnityEngine;
using DG.Tweening;
using Game.Core;
using Game.Mowers.Input;
using Game.Mowers.Models;
using Game.UndoSystem;
using Sirenix.OdinInspector;

namespace Game.Mowers
{
	internal class MowerTransformAnimator : MonoBehaviour, INeedMowerPosition, INeedPositioner
	{
		[SerializeField] private AnimationSpeedHandler animationSpeedHandler;
		[SerializeField] private AnimationCurve animEase;

		[TitleGroup("Event Channels")]
		[SerializeField] private Vector3EventChannel movedEventChannel;
		[SerializeField] private IBoolEventChannelListenerContainer isMowerTransformControlledExternallyEventChannelContainer;

		private IBoolEventChannelListener IsMowerTransformControlledExternallyEventChannel => isMowerTransformControlledExternallyEventChannelContainer.Result;

		private Positioner _positioner;
		private IMowerPosition _mowerPosition;

		private Coroutine _translationRoutine;
		private Coroutine _rotationRoutine;
		
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
			
			AnimateRotation(prevPosition, targetPosition, inInverted);
			AnimationPosition(targetPosition, inInverted);
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
				movedEventChannel.Raise(worldPosition);
			}
		}

		private IEnumerator TranslateRoutine(Vector3 to)
		{
			void SetPosition(Vector3 pos)
			{
				transform.position = pos;
				movedEventChannel.Raise(pos);
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