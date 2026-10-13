using Core;
using UnityEngine;

namespace Settings
{
	/// <summary>Sets the global Low/High shader keywords (the grass and water shaders' quality) from <see cref="GraphicsSettingState"/>.</summary>
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(ShaderQualityManager),
		menuName = SoNames.SettingsDir + nameof(ShaderQualityManager))]
	public class ShaderQualityManager : ScriptableObject
	{
		[SerializeField] private string graphicsSettingsLowShaderKeyword = "_GRAPHICSSETTINGS_LOW";
		[SerializeField] private string graphicSettingsHighShaderKeyword = "_GRAPHICSSETTINGS_HIGH";

		private void OnEnable()
		{
			OnGraphicsSettingsChanged(GraphicsSettingState.Current);
			GraphicsSettingState.Changed += OnGraphicsSettingsChanged;
		}

		private void OnDisable()
		{
			GraphicsSettingState.Changed -= OnGraphicsSettingsChanged;
		}

		[ContextMenu("Set Low")]
		public void SetLow()
		{
			Shader.DisableKeyword(graphicSettingsHighShaderKeyword);
			Shader.EnableKeyword(graphicsSettingsLowShaderKeyword);
		}

		[ContextMenu("Set High")]
		public void SetHigh()
		{
			Shader.DisableKeyword(graphicsSettingsLowShaderKeyword);
			Shader.EnableKeyword(graphicSettingsHighShaderKeyword);
		}

		private void OnGraphicsSettingsChanged(GraphicsSetting setting)
		{
			if (setting == GraphicsSetting.Low)
			{
				SetLow();
			}
			else
			{
				SetHigh();
			}
		}
	}
}
