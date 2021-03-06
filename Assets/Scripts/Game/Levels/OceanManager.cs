using System;
using System.Linq;
using BalsamicBits.Extensions;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
	internal class OceanManager : MonoBehaviour
	{
		[SerializeField] private LevelDataEventChannel levelDataEventChannel;
		[SerializeField] private Positioner positioner;
		[SerializeField] private float sizeMultiplier = 2;
		[SerializeField] private float planeDefaultSize = 10;
		[SerializeField] private Material material;
		
		private static readonly int bandingId = Shader.PropertyToID("_Banding");

		private void Awake()
		{
			levelDataEventChannel.EventRaised += LevelDataEventChannelOnEventRaised;
		}

		private void OnDestroy()
		{
			levelDataEventChannel.EventRaised -= LevelDataEventChannelOnEventRaised;
		}

		private void LevelDataEventChannelOnEventRaised(IReadOnlyLevelData level)
		{
			SetToCenter(level);
			SetSize(level);
			SetBanding();
		}

		private void SetBanding()
		{
			material.SetFloat(bandingId, transform.localScale.x * planeDefaultSize);
		}

		private void SetSize(IReadOnlyLevelData level)
		{
			int maxDimension = Mathf.Max(level.Width, level.Depth);
			transform.localScale = Vector3.one * maxDimension * sizeMultiplier / planeDefaultSize;
		}

		private void SetToCenter(IReadOnlyLevelData level)
		{
			GridVector levelCenterMin = new GridVector(
				Mathf.FloorToInt((level.Width - 1) / 2f),
				Mathf.FloorToInt((level.Depth - 1) / 2f));
			GridVector levelCenterMax = new GridVector(
				Mathf.CeilToInt((level.Width - 1) / 2f),
				Mathf.CeilToInt((level.Depth - 1) / 2f));
			Vector3 worldCenterMin = positioner.GetWorldPosition(levelCenterMin);
			Vector3 worldCenterMax = positioner.GetWorldPosition(levelCenterMax);

			float initialY = transform.position.y;
			transform.position = (worldCenterMin + worldCenterMax) * 0.5f;
			transform.position = transform.position.SetY(initialY);
		}
	}
}