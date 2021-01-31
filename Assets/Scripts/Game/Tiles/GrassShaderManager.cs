using System;
using System.Collections.Generic;
using BalsamicBits.Extensions;
using Core;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using ReadOnly = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace Game.Tiles
{
	[CreateAssetMenu(fileName = nameof(GrassShaderManager), menuName = SONames.GameDir + nameof(GrassShaderManager))]
	internal class GrassShaderManager : ScriptableObject, IUnreferencedScriptableObject
	{
		[SerializeField, InlineProperty, HideLabel] private ShaderProperty<float> windNoiseScale;
		[SerializeField, InlineProperty, HideLabel] private ShaderProperty<Vector2> windNoiseVelocity;
		[SerializeField, InlineProperty, HideLabel] private ShaderProperty<float> bendNoiseScale;

		private void OnValidate()
		{
			SetGlobals();
		}

		private void SetGlobals()
		{
			Shader.SetGlobalFloat(windNoiseScale.Id, windNoiseScale);
			Shader.SetGlobalVector(windNoiseVelocity.Id, windNoiseVelocity.Value);
			Shader.SetGlobalFloat(bendNoiseScale.Id, bendNoiseScale);
		}

		[Serializable]
		private class ShaderProperty<T>
		{
			[BoxGroup("$name"), SerializeField, OnValueChanged(nameof(SetId))]
			private string name;
			[BoxGroup("$name"), SerializeField]
			private T value;
			[BoxGroup("$name"), SerializeField, ReadOnly]
			private int id;

			public string Name => name;

			public T Value => value;

			public int Id => id;
			
			private void SetId(string name)
			{
				id = Shader.PropertyToID(name);
			}

			public static implicit operator T(ShaderProperty<T> shaderProperty)
			{
				return shaderProperty.Value;
			}
		}
	}
}