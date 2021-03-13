using System;
using Cinemachine;
using Core.EventChannels;
using Game.Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Cameras
{
	internal class IntroCameraManager : MonoBehaviour
	{
		[SerializeField] private GamePlayData gamePlayData;
		[SerializeField] private CinemachineBlendListCamera blendListCamera;

		[TitleGroup("Event Channels")]
		[SerializeField] private IVoidEventChannelListenerContainer startPlayingEventChannel;

		[SerializeField] private IVoidEventChannelListenerContainer stopPlayingEventChannel;

		private IVoidEventChannelListener StartPlayingEventChannel => startPlayingEventChannel.Result;
		private IVoidEventChannelListener StopPlayingEventChannel => stopPlayingEventChannel.Result;

		private void Awake()
		{
			var firstInstruction = blendListCamera.m_Instructions[0];
			firstInstruction.m_Hold = gamePlayData.LevelIntroDuration;
			blendListCamera.m_Instructions[0] = firstInstruction;
		}

		private void OnEnable()
		{
			StartPlayingEventChannel.EventRaised += OnStartPlaying;
			StopPlayingEventChannel.EventRaised += OnStopPlaying;
		}

		private void OnDisable()
		{
			StartPlayingEventChannel.EventRaised -= OnStartPlaying;
			StopPlayingEventChannel.EventRaised -= OnStopPlaying;
		}

		private void OnStartPlaying()
		{
			blendListCamera.enabled = false;
			blendListCamera.enabled = true;
		}

		private void OnStopPlaying()
		{
		}
	}
}