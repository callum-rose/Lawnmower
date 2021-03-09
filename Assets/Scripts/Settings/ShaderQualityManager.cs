using Core;
using Settings.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Settings
{
	[UnreferencedScriptableObject]
	[CreateAssetMenu(fileName = nameof(ShaderQualityManager),
		menuName = SONames.SettingsDir + nameof(ShaderQualityManager))]
	internal class ShaderQualityManager : ScriptableObject
	{
		[SerializeField] private string graphicsSettingsLowShaderKeyword = "_GRAPHICSSETTINGS_LOW";
		[SerializeField] private string graphicSettingsHighShaderKeyword = "_GRAPHICSSETTINGS_HIGH";
		[SerializeField] private GraphicsSetting defaultQualitySetting;

		[TitleGroup("Event Channels")]
		[SerializeField] private IGraphicsSettingEventChannelListenerContainer graphicsSettingEventChannel;

		private IGraphicsSettingEventChannelListener GraphicsSettingEventChannel => graphicsSettingEventChannel.Result;

		private void OnEnable()
		{
			GraphicsSettingEventChannel.EventRaised += OnGraphicsSettingsChanged;
			
			OnGraphicsSettingsChanged(defaultQualitySetting);
		}

		private void OnDisable()
		{
			GraphicsSettingEventChannel.EventRaised -= OnGraphicsSettingsChanged;
		}

		[Button]
		public void SetLow()
		{
			Shader.DisableKeyword(graphicSettingsHighShaderKeyword);
			Shader.EnableKeyword(graphicsSettingsLowShaderKeyword);
		}

		[Button]
		public void SetHigh()
		{
			Shader.DisableKeyword(graphicsSettingsLowShaderKeyword);
			Shader.EnableKeyword(graphicSettingsHighShaderKeyword);
		}

		[Button]
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