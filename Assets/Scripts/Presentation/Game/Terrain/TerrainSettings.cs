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
		[Tooltip("How far the top runs past the tiles as bare shore, in tiles, so the edging rocks sit clear of the grass")]
		[SerializeField, Min(0)] private float shoreWidth = 0.3f;
		[SerializeField] private float bottomHeight = -1.2f;
		[Tooltip("Horizontal run per unit of drop; 0 is a vertical wall")]
		[SerializeField, Range(0, 3)] private float slope = 0.8f;
		[SerializeField, Range(0, 0.5f)] private float edgeRadius = 0.15f;

		[Header("Noise")]
		[SerializeField, Range(0, 0.2f)] private float noiseAmplitude = 0.04f;
		[SerializeField] private float noiseFrequency = 2;
		[SerializeField, Min(0)] private float noiseFadeDepth = 0.3f;
		[SerializeField] private Vector3 noiseOffset = new Vector3(17.3f, 3.1f, 41.9f);

		[Header("Roughness")]
		[Tooltip("Depth below the pivot over which the sides get rougher, reaching full roughness at the bottom of it")]
		[SerializeField, Min(0)] private float roughnessDepth = 0.8f;
		[Tooltip("How much the slope wanders around the outline at full roughness, as a fraction of it")]
		[SerializeField, Range(0, 1)] private float slopeVariation = 0.5f;
		[SerializeField] private float slopeVariationFrequency = 0.6f;
		[Tooltip("How far large lumps push the sides in and out at full roughness, in tiles")]
		[SerializeField, Range(0, 0.5f)] private float lumpAmplitude = 0.2f;
		[SerializeField] private float lumpFrequency = 1.3f;
		[Tooltip("Scales the lumps' vertical frequency: above 1 squashes them into ledges")]
		[SerializeField, Range(0.25f, 4)] private float lumpVerticalScale = 1.8f;

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
		public float ShoreWidth => shoreWidth;
		public float BottomHeight => bottomHeight;

		public TerrainShape Shape => new TerrainShape
		{
			TopHeight = topHeight,
			PivotHeight = pivotHeight,
			Shore = shoreWidth,
			Slope = slope,
			EdgeRadius = edgeRadius,
			NoiseAmplitude = noiseAmplitude,
			NoiseFrequency = noiseFrequency,
			NoiseFadeDepth = noiseFadeDepth,
			NoiseOffset = (float3)noiseOffset,
			RoughnessDepth = roughnessDepth,
			SlopeVariation = slopeVariation,
			SlopeVariationFrequency = slopeVariationFrequency,
			LumpAmplitude = lumpAmplitude,
			LumpFrequency = lumpFrequency,
			LumpVerticalScale = lumpVerticalScale,
		};
	}
}
