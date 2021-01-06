using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Tiles
{
	[RequireComponent(typeof(ParticleSystemRenderer))]
	internal class GrassParticlesColourer : MonoBehaviour
	{
		[SerializeField] private GrassMaterialDataHolder grassMaterialDataHolder;
		[SerializeField] private Vector2 yRange = new Vector2(0, 1);

#if UNITY_EDITOR
		[ShowInInspector, Range(1, GrassTile.MaxGrassHeight)]
		private int _debugGrassheight = 3;
#endif

		private static MaterialPropertyBlock _propertyBlock;
		private static readonly int ColourFadeBaseColour = Shader.PropertyToID("_ColourFadeBaseColour");
		private static readonly int ColourFadeTipColour = Shader.PropertyToID("_ColourFadeTipColour");
		private static readonly int ColourFadeYRange = Shader.PropertyToID("_ColourFadeYRange");

		private ParticleSystemRenderer _particlesRenderer;
		private GrassMaterialDataHolder.GrassData _data;

		private void Awake()
		{
			_particlesRenderer = GetComponent<ParticleSystemRenderer>();
			_data = grassMaterialDataHolder.GetDataForHeight(3);
			SetPropertyBlock();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			Set(_debugGrassheight);
		}
#endif

		public void Set(int grassHeight)
		{
			_data = grassMaterialDataHolder.GetDataForHeight(grassHeight);
			SetPropertyBlock();
		}

		private void SetPropertyBlock()
		{
			_propertyBlock ??= new MaterialPropertyBlock();

			if (_particlesRenderer == null)
			{
				_particlesRenderer = GetComponent<ParticleSystemRenderer>();
			}

			_particlesRenderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetColor(ColourFadeBaseColour, _data.BaseColour);
			_propertyBlock.SetColor(ColourFadeTipColour, _data.TipColour);
			_propertyBlock.SetVector(ColourFadeYRange, yRange);
			_particlesRenderer.SetPropertyBlock(_propertyBlock);
		}
	}
}