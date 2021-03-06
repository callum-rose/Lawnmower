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
		[SerializeField] private float animDuration;
		[SerializeField] private float timeOffsetPerUnitDistance;
		
		[TitleGroup("Event Channels")]
		[SerializeField] private TileObjectEventChannel tileObjectCreatedEventChannel;

		private void OnEnable()
		{
			tileObjectCreatedEventChannel.EventRaised += OnTileObjectObjectCreated;
		}

		private void OnDisable()
		{
			tileObjectCreatedEventChannel.EventRaised -= OnTileObjectObjectCreated;
		}

		private void OnTileObjectObjectCreated(GameObject tile, GridVector position)
		{
			Vector3 worldPosition = tile.transform.position;
			tile.transform.position += positionOffset;

			float distanceFromOrigin = position.Magnitude;
			tile.transform.DOMove(worldPosition, animDuration).SetDelay(distanceFromOrigin * timeOffsetPerUnitDistance);
		}
	}
}