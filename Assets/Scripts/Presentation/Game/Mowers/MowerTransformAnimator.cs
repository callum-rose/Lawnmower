using System;
using System.Collections;
using System.Collections.Generic;
using BalsamicBits.Extensions;
using Core.EventChannels;
using UnityEngine;
using Game.Core;
using Game.Mowers.Input;
using Game.Mowers.Models;
using Game.UndoSystem;
using Lawnmower.Model;

namespace Game.Mowers
{
	internal class MowerTransformAnimator : MonoBehaviour, INeedMowerPosition, INeedPositioner
	{
		[SerializeField] private AnimationSpeedHandler animationSpeedHandler;
		[SerializeField] private AnimationCurve animEase;

		[Header("Spring Jumps")]
		[SerializeField, Min(0)] private float jumpHeight = 1.5f;
		[SerializeField, Min(0.05f)] private float jumpDuration = 0.45f;
		[Tooltip("Arc height of a step that climbs onto or off a raised surface (the spring's mushroom cap).")]
		[SerializeField, Min(0)] private float hopHeight = 1f;

		[Header("Rabbit Holes")]
		[SerializeField, Min(0)] private float burrowDepth = 0.8f;
		[SerializeField, Range(0.1f, 1)] private float burrowScale = 0.5f;
		[SerializeField, Min(0.05f)] private float burrowSinkDuration = 0.2f;
		[SerializeField, Min(0.05f)] private float burrowRiseDuration = 0.3f;
		[SerializeField, Min(0)] private float burrowRiseHeight = 0.6f;
		[Tooltip("How far towards a blocked exit the mower gets, in tiles, before it bonks")]
		[SerializeField, Range(0, 0.5f)] private float bonkDistance = 0.3f;
		[SerializeField, Min(0.05f)] private float bonkDuration = 0.25f;
		[SerializeField, Min(0)] private float dazedWobbleAngle = 25f;
		[SerializeField, Min(0.05f)] private float dazedDuration = 0.6f;

		[Header("Event Channels")]
		[SerializeField] private IVector3EventChannelTransmitterContainer movedEventChannel;
		[SerializeField] private IBoolEventChannelListenerContainer isMowerTransformControlledExternallyEventChannelContainer;

		private IVector3EventChannelTransmitter MovedEventChannel => movedEventChannel.Result;
		private IBoolEventChannelListener IsMowerTransformControlledExternallyEventChannel => isMowerTransformControlledExternallyEventChannelContainer.Result;

		private Positioner _positioner;
		private IMowerPosition _mowerPosition;

		private Coroutine _translationRoutine;
		private Coroutine _rotationRoutine;
		private Func<IEnumerator> _pendingRoutine;
		private Vector3? _scaleBeforeBurrow;
		
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
			_mowerPosition.Bounced += OnMowerBounced;
		}

		void INeedMowerPosition.Clear()
		{
			_mowerPosition.CurrentPosition.ValueChangedFromTo -= OnMowerMoved;
			_mowerPosition.Bounced -= OnMowerBounced;
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

			PathSegment? segment = _mowerPosition.CurrentSegment;

			if (!isActiveAndEnabled)
			{
				// Hidden (before the drop-in): coroutines can't run, so just keep the position current.
				_pendingRoutine = null;
				SnapPosition(targetPosition);
			}
			else if (!inInverted && segment?.Kind == MovementKind.Burrow)
			{
				// Down the hole once the step onto it has finished, then up out of the other one.
				PathSegment burrow = segment.Value;
				QueueTranslation(() => BurrowRoutine(burrow));
			}
			else if ((targetPosition - prevPosition).IsCardinalUnit)
			{
				_pendingRoutine = null;
				AnimateRotation(prevPosition, targetPosition, inInverted);
				AnimationPosition(prevPosition, targetPosition, inInverted);
			}
			else if (!inInverted)
			{
				// A spring jump (B3): arc to the landing tile once the step onto the spring has finished.
				Vector3 landing = _positioner.GetStandWorldPosition(targetPosition);
				QueueTranslation(() => JumpRoutine(landing));
			}
			else
			{
				// Start placement (Zero -> start) and undoing a jump or burrow: snap. This also raises the
				// mower-moved channel that MowerCreatedAnimator waits on before showing the mower.
				_pendingRoutine = null;
				SnapPosition(targetPosition);
			}
		}

		private void OnMowerBounced(IReadOnlyList<PathSegment> attempted)
		{
			if (_transformIsControlledExternally || !isActiveAndEnabled || attempted.Count == 0)
			{
				return;
			}

			QueueTranslation(() => BounceRoutine(attempted));
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

		private void AnimationPosition(GridVector prevPosition, GridVector position, Xor isInverted)
		{
			Vector3 worldPosition = _positioner.GetStandWorldPosition(position);

			StopTranslation();

			if (!isInverted)
			{
				// Climbing onto or off a raised surface arcs over its edge rather than passing through it.
				float prevY = _positioner.GetStandWorldPosition(prevPosition).y;
				float arc = Mathf.Approximately(worldPosition.y, prevY) ? 0 : hopHeight;
				_translationRoutine = ThenPending(TranslateRoutine(worldPosition, arc)).Start(this);
			}
			else
			{
				transform.position = worldPosition;
				MovedEventChannel.Raise(worldPosition);
			}
		}

		private void SnapPosition(GridVector position)
		{
			StopTranslation();

			Vector3 worldPosition = _positioner.GetStandWorldPosition(position);
			transform.position = worldPosition;
			MovedEventChannel.Raise(worldPosition);
		}

		/// <summary>Stops any translation, putting the mower back to full size if it was cut off down a hole.</summary>
		private void StopTranslation()
		{
			_translationRoutine.Stop(this);
			_translationRoutine = null;

			if (_scaleBeforeBurrow.HasValue)
			{
				transform.localScale = _scaleBeforeBurrow.Value;
				_scaleBeforeBurrow = null;
			}
		}

		private IEnumerator TranslateRoutine(Vector3 to, float arc = 0)
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
				Vector3 position = Vector3.Lerp(from, to, easedT) + Vector3.up * (arc * 4 * easedT * (1 - easedT));

				SetPosition(position);

				yield return null;
			}

			SetPosition(to);
		}

		private IEnumerator JumpRoutine(Vector3 to)
		{
			yield return Slide(transform.position, to, jumpDuration, jumpHeight);
		}

		/// <summary>Sinks into the burrow's hole and comes up out of the other, onto the landing tile.</summary>
		private IEnumerator BurrowRoutine(PathSegment burrow)
		{
			Vector3 restScale = transform.localScale;
			_scaleBeforeBurrow = restScale;
			Vector3 down = Vector3.down * burrowDepth;
			Vector3 entry = _positioner.GetStandWorldPosition(burrow.From);
			Vector3 exit = _positioner.GetStandWorldPosition(burrow.ExitHole);
			Vector3 landing = _positioner.GetStandWorldPosition(burrow.To);

			yield return Slide(entry, entry + down, burrowSinkDuration, 0, restScale, restScale * burrowScale);
			transform.rotation = LookAlong(burrow.Direction);
			yield return Slide(exit + down, landing, burrowRiseDuration, burrowRiseHeight, restScale * burrowScale, restScale);
			_scaleBeforeBurrow = null;
		}

		/// <summary>
		/// Down the hole, up the other one into a blocked exit, bonk, back down and out of the first hole to where the
		/// move started, dazed.
		/// </summary>
		private IEnumerator BounceRoutine(IReadOnlyList<PathSegment> attempted)
		{
			Vector3 restScale = transform.localScale;
			_scaleBeforeBurrow = restScale;
			Vector3 shrunk = restScale * burrowScale;
			Vector3 down = Vector3.down * burrowDepth;
			Vector3 start = _positioner.GetStandWorldPosition(attempted[0].From);

			for (int i = 0; i < attempted.Count - 1; i++)
			{
				PathSegment segment = attempted[i];
				Vector3 to = _positioner.GetStandWorldPosition(segment.To);

				if (segment.Kind == MovementKind.Step)
				{
					transform.rotation = LookAlong(segment.Direction);
					yield return TranslateRoutine(to);
				}
				else
				{
					yield return JumpRoutine(to);
				}
			}

			PathSegment burrow = attempted[attempted.Count - 1];
			Vector3 entry = _positioner.GetStandWorldPosition(burrow.From);
			Vector3 exit = _positioner.GetStandWorldPosition(burrow.ExitHole);
			Vector3 bonk = exit + (_positioner.GetStandWorldPosition(burrow.To) - exit) * bonkDistance;

			yield return Slide(entry, entry + down, burrowSinkDuration, 0, restScale, shrunk);
			transform.rotation = LookAlong(burrow.Direction);
			yield return Slide(exit + down, bonk, burrowRiseDuration, 0, shrunk, restScale);
			yield return Slide(bonk, exit, bonkDuration, 0);
			yield return Slide(exit, exit + down, burrowSinkDuration, 0, restScale, shrunk);

			// The first leg is always the input step: head back the way it came.
			transform.rotation = LookAlong(attempted[0].Direction * -1);
			yield return Slide(entry + down, entry, burrowRiseDuration, 0, shrunk, restScale);
			_scaleBeforeBurrow = null;
			yield return TranslateRoutine(start);
			yield return DazedRoutine();
		}

		private IEnumerator DazedRoutine()
		{
			Quaternion rest = transform.rotation;

			for (float time = Time.deltaTime; time < dazedDuration; time += Time.deltaTime)
			{
				float t = time / dazedDuration;
				float yaw = Mathf.Sin(t * Mathf.PI * 4) * dazedWobbleAngle * (1 - t);
				transform.rotation = rest * Quaternion.AngleAxis(yaw, Vector3.up);

				yield return null;
			}

			transform.rotation = rest;
		}

		/// <summary>Moves (and optionally scales) the mower in a straight line, lifted by an arc of <paramref name="arc"/>.</summary>
		private IEnumerator Slide(Vector3 from, Vector3 to, float duration, float arc,
			Vector3? fromScale = null, Vector3? toScale = null)
		{
			for (float time = Time.deltaTime; time < duration; time += Time.deltaTime)
			{
				float t = time / duration;
				Vector3 position = Vector3.Lerp(from, to, t) + Vector3.up * (arc * 4 * t * (1 - t));

				if (fromScale.HasValue && toScale.HasValue)
				{
					transform.localScale = Vector3.Lerp(fromScale.Value, toScale.Value, t);
				}

				transform.position = position;
				MovedEventChannel.Raise(position);

				yield return null;
			}

			if (toScale.HasValue)
			{
				transform.localScale = toScale.Value;
			}

			transform.position = to;
			MovedEventChannel.Raise(to);
		}

		/// <summary>Starts the routine now, or after the current translation if one is running.</summary>
		private void QueueTranslation(Func<IEnumerator> routine)
		{
			if (_translationRoutine != null)
			{
				_pendingRoutine = routine;
				return;
			}

			_translationRoutine = ThenPending(routine()).Start(this);
		}

		private IEnumerator ThenPending(IEnumerator routine)
		{
			yield return routine;

			_translationRoutine = null;

			if (_pendingRoutine != null)
			{
				Func<IEnumerator> next = _pendingRoutine;
				_pendingRoutine = null;
				_translationRoutine = ThenPending(next()).Start(this);
			}
		}

		private static Quaternion LookAlong(GridVector direction)
		{
			return Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);
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