using System;
using Core;
using Core.EventChannels;
using IUnified;
using UnityEngine;
using Lawnmower.Model;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelDataEventChannel),
		menuName = SoNames.GameDir + nameof(LevelDataEventChannel))]
	internal sealed class LevelDataEventChannel : BaseEventChannel<Level>, ILevelDataEventChannelListener,
		ILevelDataEventChannelTransmitter
	{
		protected override bool ShouldBeSolo => true;
		protected override bool PushLastDataOnSubscribe => true;
	}

	internal interface ILevelDataEventChannelListener : IEventChannelListener<Level> { }
	
	internal interface ILevelDataEventChannelTransmitter : IEventChannelTransmitter<Level> { }
	
	[Serializable]
	internal class ILevelDataEventChannelListenerContainer : IUnifiedContainer<ILevelDataEventChannelListener> { }
	
	[Serializable]
	internal class ILevelDataEventChannelTransmitterContainer : IUnifiedContainer<ILevelDataEventChannelTransmitter> { }
}