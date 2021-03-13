using System;
using Core;
using Core.EventChannels;
using IUnified;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelDataEventChannel),
		menuName = SoNames.GameDir + nameof(LevelDataEventChannel))]
	internal sealed class LevelDataEventChannel : BaseEventChannel<IReadOnlyLevelData>, ILevelDataEventChannelListener,
		ILevelDataEventChannelTransmitter
	{
		protected override bool ShouldBeSolo => true;
	}

	internal interface ILevelDataEventChannelListener : IEventChannelListener<IReadOnlyLevelData> { }
	
	internal interface ILevelDataEventChannelTransmitter : IEventChannelTransmitter<IReadOnlyLevelData> { }
	
	[Serializable]
	internal class ILevelDataEventChannelListenerContainer : IUnifiedContainer<ILevelDataEventChannelListener> { }
	
	[Serializable]
	internal class ILevelDataEventChannelTransmitterContainer : IUnifiedContainer<ILevelDataEventChannelTransmitter> { }
}