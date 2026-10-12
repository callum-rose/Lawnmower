using Core;
using System.Collections.Generic;
using BalsamicBits.Extensions;
using UnityEngine;

namespace Game.Mowers.Models
{
	[ExecuteInEditMode]
	internal class FaceOffsetComponent : MonoBehaviour
	{
		[SerializeField] private Material material;

		[SerializeField, Dropdown(nameof(ShaderPropertyNames))]
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
			
			material.SetTextureOffset(_texturePropertyId.Value, offset + baseOffset);
		}
	}
}