using Settings;
using UnityEngine;

namespace Game.Cameras
{
	/// <summary>Only renders the depth texture (soft water edges) on High graphics.</summary>
	internal class GameCameraSettingsManager : MonoBehaviour
	{
		[SerializeField] private Camera camera;

		private void OnEnable()
		{
			OnGraphicsSettingChanged(GraphicsSettingState.Current);
			GraphicsSettingState.Changed += OnGraphicsSettingChanged;
		}

		private void OnDisable()
		{
			GraphicsSettingState.Changed -= OnGraphicsSettingChanged;
		}

		private void OnGraphicsSettingChanged(GraphicsSetting setting)
		{
			camera.depthTextureMode = setting == GraphicsSetting.High ? DepthTextureMode.Depth : DepthTextureMode.None;
		}
	}
}
