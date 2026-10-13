using Core;
using Settings.EventChannels;
using UnityEngine;

namespace Settings
{
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(ShaderQualityManager),
		menuName = SoNames.SettingsDir + nameof(ShaderQualityManager))]
	public class ShaderQualityManager : ScriptableObject
	{
		[SerializeField] private string graphicsSettingsLowShaderKeyword = "_GRAPHICSSETTINGS_LOW";
		[SerializeField] private string graphicSettingsHighShaderKeyword = "_GRAPHICSSETTINGS_HIGH";
		[SerializeField] private GraphicsSetting defaultQualitySetting;

		[Header("Event Channels")]
		[SerializeField] private IGraphicsSettingEventChannelListenerContainer graphicsSettingEventChannel;

		private IGraphicsSettingEventChannelListener GraphicsSettingEventChannel => graphicsSettingEventChannel.Result;

		private void OnEnable()
		{
			OnGraphicsSettingsChanged(defaultQualitySetting);
			GraphicsSettingEventChannel.EventRaised += OnGraphicsSettingsChanged;
		}

		private void OnDisable()
		{
			GraphicsSettingEventChannel.EventRaised -= OnGraphicsSettingsChanged;
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

		[ContextMenu("Toggle")]
		public void Toggle()
		{
			bool isLow = Shader.IsKeywordEnabled(graphicsSettingsLowShaderKeyword);
			if (isLow)
			{
				SetHigh();
			}
			else
			{
				SetLow();
			}
		}

		private void OnGraphicsSettingsChanged(GraphicsSetting setting)
		{
			switch (setting)
			{
				case GraphicsSetting.Low:
					SetLow();
					break;
				
				case GraphicsSetting.High:
					SetHigh();
					break;
				
				default:
					Debug.LogError($"GraphicsSetting wrong somehow: {setting}");
					goto case GraphicsSetting.Low;
			}
		}
	}
}