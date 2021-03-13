using Settings;
using Settings.EventChannels;
using UnityEngine;

namespace Game.Cameras
{
	internal class GameCameraSettingsManager : MonoBehaviour
	{
		[SerializeField] private Camera camera;
		
		[SerializeField] private IGraphicsSettingEventChannelListenerContainer graphicsSettingEventChannel;

		private IGraphicsSettingEventChannelListener GraphicsSettingEventChannel => graphicsSettingEventChannel.Result;

		private void OnEnable()
		{
			OnGraphicsSettingChanged(GraphicsSetting.High);
			GraphicsSettingEventChannel.EventRaised += OnGraphicsSettingChanged;
		}

		private void OnDisable()
		{
			GraphicsSettingEventChannel.EventRaised -= OnGraphicsSettingChanged;
		}

		private void OnGraphicsSettingChanged(GraphicsSetting setting)
		{
			switch (setting)
			{
				default:
				case GraphicsSetting.Low:
					camera.depthTextureMode = DepthTextureMode.None;
					break;
				case GraphicsSetting.High:
					camera.depthTextureMode = DepthTextureMode.Depth;
					break;
			}
		}
	}
}