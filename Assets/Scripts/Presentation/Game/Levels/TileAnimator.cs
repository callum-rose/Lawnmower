using Core;
using Core.EventChannels;
using DG.Tweening;
using Game.Core;
using UnityEngine;
using Lawnmower.Model;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(TileAnimator), menuName = SoNames.GameDir + nameof(TileAnimator))]
	[UnreferencedScriptableObject]
	internal class TileAnimator : ScriptableObject
	{
		[SerializeField] private Vector3 positionOffset;
		[SerializeField] private float perTileAnimDuration = 0.5f;
		[SerializeField] private GamePlayData gamePlayData;

		[Header("Event Channels")]
		[SerializeField] private ILevelDataEventChannelListenerContainer levelStartedEventChannelContainer;

		[SerializeField] private TileObjectEventChannel tileObjectCreatedEventChannel;
		[SerializeField] private IVoidEventChannelTransmitterContainer tileAnimationsFinishedEventChannel;

		private float MaxDelay => gamePlayData.LevelIntroDuration - perTileAnimDuration;

		private ILevelDataEventChannelListener LevelStartedEventChannel => levelStartedEventChannelContainer.Result;

		private IVoidEventChannelTransmitter TileAnimationsFinishedEventChannel =>
			tileAnimationsFinishedEventChannel.Result;

		private float _furthestTile = 999f;

		private int _tileAnimCount;

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

			float distanceFromOrigin = (float)position.Magnitude;
			float normalisedDistance = distanceFromOrigin / _furthestTile;
			float delay = normalisedDistance * gamePlayData.LevelIntroDuration;

			_tileAnimCount++;
			tile.transform.DOMove(worldPosition, perTileAnimDuration)
				.SetDelay(delay)
				.OnComplete(() =>
				{
					_tileAnimCount--;

					if (_tileAnimCount > 0)
					{
						return;
					}

					TileAnimationsFinishedEventChannel.Raise();
				});
		}

		private void OnLevelStarted(Level level)
		{
			_tileAnimCount = 0;
			_furthestTile = (float)new GridVector(level.Width - 1, level.Depth - 1).Magnitude;
		}
	}
}