using UnityEngine;
using UnityEngine.Serialization;

namespace Core
{
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(NoiseShaderManager), menuName = SoNames.CoreDir + nameof(NoiseShaderManager))]
	internal class NoiseShaderManager : ScriptableObject
	{
		[FormerlySerializedAs("texture"),SerializeField] private ShaderProperty<Texture2D> noiseTexture;

		private void OnEnable()
		{
			noiseTexture.SetId();
			noiseTexture.Apply();
		}
	}
}