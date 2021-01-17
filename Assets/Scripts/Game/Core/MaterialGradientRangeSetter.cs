using System.Collections.Generic;
using BalsamicBits.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Core
{
	[RequireComponent(typeof(Renderer))]
	public class MaterialGradientRangeSetter : MonoBehaviour
	{
		[SerializeField] private Vector2 range;

		[SerializeField, ValueDropdown(nameof(MaterialPropertyNames), HideChildProperties = true)]
		private string shaderRangePropertyName;

		private List<string> MaterialPropertyNames
		{
			get
			{
				if (_renderer == null)
				{
					_renderer = GetComponent<Renderer>();
				}

				return _renderer.sharedMaterial.shader.GetPropertyNames();
			}
		}

		private static MaterialPropertyBlock _materialPropertyBlock;

		private Renderer _renderer;

		private void Awake()
		{
			_materialPropertyBlock ??= new MaterialPropertyBlock();

			_renderer = GetComponent<Renderer>();
		}

		private void OnEnable()
		{
			SetPropertyBlock();
		}

		private void OnValidate()
		{
			if (enabled)
			{
				SetPropertyBlock();
			}
		}

		[Button, EnableIf(nameof(enabled))]
		private void SetPropertyBlock()
		{
#if UNITY_EDITOR
			if (_renderer == null)
			{
				_renderer = GetComponent<Renderer>();
			}
			
			_materialPropertyBlock ??= new MaterialPropertyBlock();
#endif

			_renderer.GetPropertyBlock(_materialPropertyBlock);
			_materialPropertyBlock.SetVector(shaderRangePropertyName, range);
			_renderer.SetPropertyBlock(_materialPropertyBlock);
		}
	}
}