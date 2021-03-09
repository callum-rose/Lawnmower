using System;
using BalsamicBits.Extensions;
using Game.Core;
using Settings;
using Settings.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Game.Levels
{
	internal class OceanManager : MonoBehaviour
	{
		[SerializeField] private Positioner positioner;
		[SerializeField] private int sizeOffset = 2;
		[SerializeField] private float planeDefaultSize = 10;
		[SerializeField] private Material material;

		[TitleGroup("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelDataEventChannel;
		
		private static readonly int bandingId = Shader.PropertyToID("_Banding");

		#region Unity

		private void Awake()
		{
			levelDataEventChannel.EventRaised += LevelDataEventChannelOnEventRaised;
		}

		private void OnDestroy()
		{
			levelDataEventChannel.EventRaised -= LevelDataEventChannelOnEventRaised;
		}

		#endregion

		#region Events

		private void LevelDataEventChannelOnEventRaised(IReadOnlyLevelData level)
		{
			Vector3 worldCenter = GetWorldCenter(level);
			SetToCenter(worldCenter);

			float maxDistanceFromCenter = GetMaxDistanceFromCenter(level, worldCenter);
			float planeRadius = CalculatePlaneRadius(maxDistanceFromCenter);
			SetSize(planeRadius);

			SetBanding();
		}
		
		#endregion

		#region Methods

		private void SetBanding()
		{
			material.SetFloat(bandingId, transform.localScale.x * planeDefaultSize + 1);
		}

		private void SetSize(float planeRadius)
		{
			transform.localScale = Vector3.one * planeRadius * 2 / planeDefaultSize;
		}

		private void SetToCenter(Vector3 worldCenter)
		{
			float initialY = transform.position.y;
			transform.position = worldCenter;
			transform.position = transform.position.SetY(initialY);
		}

		private float CalculatePlaneRadius(float maxDistance)
		{
			float maxDistanceRoundedUpToNearestTileSize =
				Mathf.Ceil(maxDistance / LevelDimensions.TileSize) * LevelDimensions.TileSize;

			float planeRadius = maxDistanceRoundedUpToNearestTileSize + sizeOffset;
			return planeRadius;
		}

		private float GetMaxDistanceFromCenter(IReadOnlyLevelData level, Vector3 worldCenter)
		{
			float maxDistanceSqr = 0;
			Loops.TwoD(level.Width, level.Depth, (x, y) =>
			{
				Vector3 worldPosition = positioner.GetWorldPosition(new GridVector(x, y));
				float distanceSqr = Vector3.SqrMagnitude(worldPosition - worldCenter);

				if (distanceSqr > maxDistanceSqr)
				{
					maxDistanceSqr = distanceSqr;
				}
			});

			float maxDistance = Mathf.Sqrt(maxDistanceSqr);
			return maxDistance;
		}

		private Vector3 GetWorldCenter(IReadOnlyLevelData level)
		{
			GridVector levelCenterMin = new GridVector(
				Mathf.FloorToInt((level.Width - 1) / 2f),
				Mathf.FloorToInt((level.Depth - 1) / 2f));
			GridVector levelCenterMax = new GridVector(
				Mathf.CeilToInt((level.Width - 1) / 2f),
				Mathf.CeilToInt((level.Depth - 1) / 2f));

			Vector3 worldCenterMin = positioner.GetWorldPosition(levelCenterMin);
			Vector3 worldCenterMax = positioner.GetWorldPosition(levelCenterMax);

			return (worldCenterMin + worldCenterMax) * 0.5f;
		}

		#endregion
	}
}