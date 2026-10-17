using System.Collections.Generic;
using BalsamicBits.Extensions;
using Game.Core;
using UnityEngine;
using Lawnmower.Model;

namespace Game.Levels
{
	/// <summary>
	/// The circle around each level that the intro camera frames and pond decor is scattered in. The water itself is the
	/// sea built by PondManager; the intro timeline animates how far out from the centre the circle is showing.
	/// </summary>
	[ExecuteInEditMode]
	internal class OceanManager : MonoBehaviour
	{
		[SerializeField] private Positioner positioner;
		[SerializeField] private int sizeOffset = 2;
		[SerializeField] private float planeDefaultSize = 10;
		
		[SerializeField, Range(0,1)] private float amountShowingRadially;

		[Header("Defaults")]
		[SerializeField] private Vector3 defaultWorldCenter;

		[Header("Defaults")] [SerializeField]
		private float defaultRadius;

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelDataEventChannel;

		[SerializeField, HideInInspector] private Transform[] cameraTargets;

		public IEnumerable<Transform> CameraTargets => cameraTargets;

		/// <summary>How far from its centre the ocean is showing; the intro timeline animates it outwards.</summary>
		public float ShowingRadius => amountShowingRadially * transform.localScale.x * planeDefaultSize / 2;

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

		private void OnDestroy()
		{
			levelDataEventChannel.EventRaised -= LevelDataEventChannelOnEventRaised;
		}

		#endregion

		#region Events

		private void LevelDataEventChannelOnEventRaised(Level level)
		{
			SetToCenter(positioner.GetLevelWorldCenter(level));
			SetSize(GetRadius(level));
		}

		#endregion

		#region API

		/// <summary>The radius of the ocean around <paramref name="level"/>, centred on the level's world centre.</summary>
		public float GetRadius(Level level)
		{
			Vector3 worldCenter = positioner.GetLevelWorldCenter(level);
			return CalculatePlaneRadius(positioner.GetMaxDistanceFromLevelCenter(level, worldCenter));
		}

		#endregion

		#region Methods

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

		[ContextMenu("Set Defaults")]
		private void SetDefaults()
		{
			SetToCenter(defaultWorldCenter);
			SetSize(defaultRadius);
		}

		#endregion
	}
}