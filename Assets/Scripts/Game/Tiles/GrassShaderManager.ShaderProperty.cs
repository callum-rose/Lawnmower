using System;
using UnityEngine;

namespace Game.Tiles
{
	partial class GrassShaderManager
	{
		[Serializable]
		private class ShaderProperty<T> : ShaderPropertyBase
		{
			[SerializeField]
			private T value;

			public void SetAndApply(T value)
			{
				this.value = value;
				Apply();
			}
			
			public override void Apply()
			{
				switch (value)
				{
					case float f:
						Shader.SetGlobalFloat(Id, f);
						break;
					case Vector2 v2:
						Shader.SetGlobalVector(Id, v2);
						break;
					case Vector3 v3:
						Shader.SetGlobalVector(Id, v3);
						break;
					case Vector4 v4:
						Shader.SetGlobalVector(Id, v4);
						break;
				}
			}
		}

		[Serializable]
		internal abstract class ShaderPropertyBase
		{
			[SerializeField]
			private string name;

			protected int Id { get; private set; }

			public void SetId()
			{
				Id = Shader.PropertyToID(name);
			}

			public abstract void Apply();
		}
	}
}