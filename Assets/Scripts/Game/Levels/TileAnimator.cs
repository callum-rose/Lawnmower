using System;
using Core;
using Core.EventChannels;
using DG.Tweening;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(TileAnimator), menuName = SONames.GameDir + nameof(TileAnimator))]
	[UnreferencedScriptableObject]
	internal class TileAnimator : ScriptableObject
	{
		[SerializeField] private Vector3 positionOffset;
		[SerializeField] private float perTileAnimDuration = 0.5f;
		[SerializeField] private float maxDelay = 2;

		[TitleGroup("Event Channels")]
		[SerializeField] private ILevelDataEventChannelListenerContainer levelStartedEventChannelContainer;

		[SerializeField] private TileObjectEventChannel tileObjectCreatedEventChannel;

		[ShowInInspector] private float TotalAnimDuration => perTileAnimDuration + maxDelay;

		private ILevelDataEventChannelListener LevelStartedEventChannel => levelStartedEventChannelContainer.Result;

		private float _furthestTile = 999f;

		private void OnEnable()
		{
			LevelStartedEventChannel.EventRaised += OnLevelStarted;
			tileObjectCreatedEventChannel.EventRaised += OnTileObjectObjectCreated;
		}

		private void OnDisable()
		{
			LevelStartedEventChannel.EventRaised -= OnLevelStarted;
			tileObjectCreatedEventChannel.EventRaised -= OnTileObjectObjectCreated;
		}

		private void OnTileObjectObjectCreated(GameObject tile, GridVector position)
		{
			Vector3 worldPosition = tile.transform.position;
			tile.transform.position += positionOffset;

			float distanceFromOrigin = position.Magnitude;
			float normalisedDistance = distanceFromOrigin / _furthestTile;
			float delay = normalisedDistance * maxDelay;

			tile.transform.DOMove(worldPosition, perTileAnimDuration).SetDelay(delay);
		}

		private void OnLevelStarted(IReadOnlyLevelData level)
		{
			_furthestTile = new GridVector(level.Width - 1, level.Depth - 1).Magnitude;
		}
	}
}