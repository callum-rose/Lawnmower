using System;
using Core;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
	[CreateAssetMenu(fileName = nameof(TerrainSettings), menuName = SoNames.GameDir + nameof(TerrainSettings))]
	internal class TerrainSettings : ScriptableObject
	{
		[Header("Grid")]
		[SerializeField, Min(0.02f)] private float voxelSize = 0.0625f;
		[Tooltip("How far past the level's edge the mesh extends, in tiles")]
		[SerializeField, Min(0)] private float margin = 1;

		[Header("Outline")]
		[SerializeField, Range(0, 0.5f)] private float convexCornerRadius = 0.35f;
		[SerializeField, Range(0, 0.5f)] private float concaveCornerRadius = 0.25f;

		[Header("Profile")]
		[SerializeField] private float topHeight = -0.01f;
		[Tooltip("Where the sides cross the tile grid exactly: above it they're inset, below it they spread outwards")]
		[SerializeField] private float pivotHeight = -0.1f;
		[SerializeField] private float bottomHeight = -1.2f;
		[Tooltip("Horizontal run per unit of drop; 0 is a vertical wall")]
		[SerializeField, Range(0, 3)] private float slope = 0.8f;
		[SerializeField, Range(0, 0.5f)] private float edgeRadius = 0.15f;

		[Header("Noise")]
		[SerializeField, Range(0, 0.2f)] private float noiseAmplitude = 0.04f;
		[SerializeField] private float noiseFrequency = 2;
		[SerializeField, Min(0)] private float noiseFadeDepth = 0.3f;
		[SerializeField] private Vector3 noiseOffset = new Vector3(17.3f, 3.1f, 41.9f);

#if UNITY_EDITOR
		/// <summary>Raised when the settings are edited, so the terrain can be tuned live in play mode.</summary>
		public event Action Changed;

		private void OnValidate()
		{
			Changed?.Invoke();
		}
#endif

		public float VoxelSize => voxelSize;
		public float Margin => margin;
		public float ConvexCornerRadius => convexCornerRadius;
		public float ConcaveCornerRadius => concaveCornerRadius;
		public float TopHeight => topHeight;
		public float BottomHeight => bottomHeight;

		public TerrainShape Shape => new TerrainShape
		{
			TopHeight = topHeight,
			PivotHeight = pivotHeight,
			Slope = slope,
			EdgeRadius = edgeRadius,
			NoiseAmplitude = noiseAmplitude,
			NoiseFrequency = noiseFrequency,
			NoiseFadeDepth = noiseFadeDepth,
			NoiseOffset = (float3)noiseOffset,
		};
	}
}
