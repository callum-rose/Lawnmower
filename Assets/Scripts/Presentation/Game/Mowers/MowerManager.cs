using System;
using Game.Mowers.Input;
using System.Linq;
using Core.EventChannels;
using Game.Core;
using Lawnmower.Model;
using Lawnmower.Presenters;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Mowers
{
	/// <summary>The mower view: spawns the mower prefab and replays presenter calls as mower motion.</summary>
	internal class MowerManager : MonoBehaviour, IMowerView
	{
		[Header("Scene")]
		[SerializeField] private MowerObjectCreator mowerObjectCreator;
		[SerializeField] private INeedMowerPositionContainer[] mowerPositionRequirers;

		[Header("Event Channels")]
		[SerializeField] private IGameObjectEventChannelTransmitterContainer mowerCreatedEventChannelContainer;
		[SerializeField] private IGameObjectEventChannelTransmitterContainer mowerWillBeDestroyedEventChannelContainer;

		private IGameObjectEventChannelTransmitter MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;
		private IGameObjectEventChannelTransmitter MowerWillBeDestroyedEventChannel => mowerWillBeDestroyedEventChannelContainer.Result;

		private MowerData _mowerData;
		private MowerObject _mowerObject;
		private MowerMotion _motion;

		/// <summary>True once the mower has been dropped in and is visible (it starts hidden during the level intro).</summary>
		public bool IsMowerVisible => _mowerObject != null && _mowerObject.gameObject.activeInHierarchy;

		/// <summary>The mower's world position (the level origin before one is shown).</summary>
		public Vector3 GetMowerWorldPosition() => _mowerObject != null ? _mowerObject.transform.position : transform.position;

		/// <summary>The mower prefab <see cref="Show"/> spawns.</summary>
		public void SetMower(MowerData mower)
		{
			_mowerData = mower;
		}

		#region IMowerView

		public void Show(GridVector start)
		{
			if (_mowerData == null)
			{
				throw new InvalidOperationException("SetMower must be called before Show");
			}

			Hide();

			_motion = new MowerMotion();
			_mowerObject = mowerObjectCreator.Create(_mowerData, _motion);

			InitObjectsNeedingMowerPosition(_motion);
			InitCollider(_mowerObject.gameObject);

			MowerCreatedEventChannel.Raise(_mowerObject.gameObject);

			// Inverted, so the views snap to the start tile; MowerCreatedAnimator waits for this before dropping the mower in.
			_motion.Publish(GridVector.Zero, start, true);
		}

		public void PlayMove(MoveResult move)
		{
			foreach (PathSegment segment in move.Segments)
			{
				_motion?.Publish(segment.From, segment.To, false);
			}
		}

		public void RewindMove(MoveResult move)
		{
			_motion?.Publish(move.To, move.From, true);
		}

		public void PlayBump(MoveResult bump)
		{
			// The mower has no bump animation yet; G1 adds the bump haptic/SFX from the model event.
		}

		public void Hide()
		{
			if (_mowerObject == null)
			{
				_motion = null;
				return;
			}

			MowerWillBeDestroyedEventChannel.Raise(_mowerObject.gameObject);

			foreach (INeedMowerPosition mpr in mowerPositionRequirers.Select(c => c.Result))
			{
				mpr.Clear();
			}

			_mowerObject.Dispose();
			Destroy(_mowerObject.gameObject);
			_mowerObject = null;
			_motion = null;
		}

		#endregion

		private static void InitCollider(GameObject gameObject)
		{
			SphereCollider collider = gameObject.AddComponent<SphereCollider>();
			collider.radius = TileSize * 0.5f;
			collider.center = new Vector3(0, TileSize * 0.5f, 0);
		}

		private void InitObjectsNeedingMowerPosition(IMowerPosition position)
		{
			foreach (INeedMowerPosition mpr in mowerPositionRequirers.Select(c => c.Result))
			{
				mpr.Set(position);
			}
		}
	}
}
