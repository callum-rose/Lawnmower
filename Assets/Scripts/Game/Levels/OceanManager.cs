using System.Collections.Generic;
using BalsamicBits.Extensions;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
	[ExecuteInEditMode]
	internal class OceanManager : MonoBehaviour
	{
		[SerializeField] private Positioner positioner;
		[SerializeField] private int sizeOffset = 2;
		[SerializeField] private float planeDefaultSize = 10;
		
		[TitleGroup("Material")]
		[SerializeField] private Material material;
		[SerializeField, Range(0,1)] private float amountShowingRadially;
		[SerializeField] private float waveSpeed;

		[TitleGroup("Defaults")]
		[SerializeField] private Vector3 defaultWorldCenter;

		[TitleGroup("Defaults")] [SerializeField]
		private float defaultRadius;

		[TitleGroup("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelDataEventChannel;

		[SerializeField, HideInInspector] private Transform[] cameraTargets;

		public IEnumerable<Transform> CameraTargets => cameraTargets;

		private static readonly int bandingId = Shader.PropertyToID("_Banding");
		private static readonly int radialShowingId = Shader.PropertyToID("_AmountRadiallyShowing");
		private static readonly int waveSpeedId = Shader.PropertyToID("_WaveSpeed");
		
		#region Unity

		private void Awake()
		{
			SetDefaults();
			levelDataEventChannel.EventRaised += LevelDataEventChannelOnEventRaised;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.cyan;
			foreach (Transform target in cameraTargets)
			{
				Gizmos.DrawSphere(target.position, 0.25f);
			}
		}

		private void Update()
		{
			SetMaterialProperties();
		}

		private void OnDestroy()
		{
			levelDataEventChannel.EventRaised -= LevelDataEventChannelOnEventRaised;
		}

		#endregion

		#region Events

		private void LevelDataEventChannelOnEventRaised(IReadOnlyLevelData level)
		{
			Vector3 worldCenter = positioner.GetLevelWorldCenter(level);
			SetToCenter(worldCenter);

			float maxDistanceFromCenter = positioner.GetMaxDistanceFromLevelCenter(level, worldCenter);
			float planeRadius = CalculatePlaneRadius(maxDistanceFromCenter);
			SetSize(planeRadius);

			SetMaterialProperties();
		}

		#endregion

		#region Methods

		private void SetMaterialProperties()
		{
			material.SetFloat(bandingId, transform.localScale.x * planeDefaultSize + 1);
			material.SetFloat(radialShowingId, amountShowingRadially);
			material.SetFloat(waveSpeedId, waveSpeed);
		}

		private void SetSize(float planeRadius)
		{
			transform.localScale = Vector3.one * planeRadius * 2 / planeDefaultSize;
		}

		private void SetToCenter(Vector3 worldCenter)
		{
			float initialY = transform.position.y;
			transform.localPosition = worldCenter.SetY(initialY);
		}

		private float CalculatePlaneRadius(float maxDistance)
		{
			float maxDistanceRoundedUpToNearestTileSize =
				Mathf.Ceil(maxDistance / LevelDimensions.TileSize) * LevelDimensions.TileSize;

			return maxDistanceRoundedUpToNearestTileSize + sizeOffset;
		}

#if UNITY_EDITOR
		[TitleGroup("Camera Targets"), Button, HideInPlayMode]
		private void CreateCircumferencialTargetTransforms(int count)
		{
			if (cameraTargets == null || count != cameraTargets.Length)
			{
				transform.DestroyAllChildrenImmediate();

				cameraTargets = new Transform[count];
				for (int i = 0; i < count; i++)
				{
					Transform newTarget = new GameObject("Target " + i).transform;
					newTarget.SetParent(transform);
					cameraTargets[i] = newTarget;
				}
			}

			for (int i = 0; i < count; i++)
			{
				Transform target = cameraTargets[i];
				float angle = 2f * Mathf.PI * i / count;
				target.localPosition = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * planeDefaultSize / 2;
			}
		}
#endif

		[TitleGroup("Defaults"), Button("Set Defaults")]
		private void SetDefaults()
		{
			SetToCenter(defaultWorldCenter);
			SetSize(defaultRadius);
			SetMaterialProperties();
		}

		#endregion
	}
}