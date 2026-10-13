using System.Collections.Generic;
using BalsamicBits.Extensions;
using UnityEngine;

namespace Game.Mowers.Models
{
	[ExecuteInEditMode]
	internal class FaceOffsetComponent : MonoBehaviour
	{
		[SerializeField] private Material material;

		[SerializeField]
		private string texturePropertyName;

		[SerializeField] private Vector2 baseOffset;
		[SerializeField] private Vector2 offset;

		private List<string> ShaderPropertyNames =>
			material != null ? material.shader.GetPropertyNames() : new List<string>();

		private int? _texturePropertyId;

		private void Awake()
		{
			_texturePropertyId = Shader.PropertyToID(texturePropertyName);
		}

		private void OnEnable()
		{
			SetOffset();
		}

		private void OnValidate()
		{
			SetOffset();
		}

		private void Update()
		{
			SetOffset();
		}

		private void SetOffset()
		{
#if UNITY_EDITOR
			_texturePropertyId ??= Shader.PropertyToID(texturePropertyName);
#endif
			
			Vector2 totalOffset = offset + baseOffset;

			if (material.HasTexture(_texturePropertyId.Value))
			{
				material.SetTextureOffset(_texturePropertyId.Value, totalOffset);
				return;
			}

			// Procedural faces store tiling/offset in a vector (xy tiling, zw offset)
			Vector4 tilingOffset = material.GetVector(_texturePropertyId.Value);
			material.SetVector(
				_texturePropertyId.Value,
				new Vector4(tilingOffset.x, tilingOffset.y, totalOffset.x, totalOffset.y));
		}
	}
}