using Core.EventChannels;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	internal class LevelCameraManager : MonoBehaviour
	{
		[SerializeField] private MixCameraAnimator mixCameraAnimator;
		[SerializeField] private int levelCamIndex;
		[SerializeField] private int gameCamIndex;

		[TitleGroup("Event Channels")]
		[SerializeField] private IBoolEventChannelTransmitterContainer canPanCameraEventChannel;

		private bool _currentLookAt;

		private IBoolEventChannelTransmitter CanPanCameraEventChannel => canPanCameraEventChannel.Result;

		public void DoLookAtLevel(bool lookAt)
		{
			mixCameraAnimator.AnimateTo(lookAt ? levelCamIndex : gameCamIndex);

			bool canPan = !lookAt;
			CanPanCameraEventChannel.Raise(canPan);

			_currentLookAt = lookAt;
		}

		public void ToggleLookAt()
		{
			DoLookAtLevel(!_currentLookAt);
		}
	}
}