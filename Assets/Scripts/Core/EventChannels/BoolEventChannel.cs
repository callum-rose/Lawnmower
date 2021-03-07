using System;
using IUnified;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(BoolEventChannel), menuName = SONames.CoreDir + nameof(BoolEventChannel))]
	public class BoolEventChannel : BaseEventChannel<bool>, IBoolEventChannelListener, IBoolEventChannelTransmitter
	{
		protected override bool ShouldBeSolo => true;
	}
	
	public interface IBoolEventChannelListener : IEventChannelListener<bool> { }
	
	public interface IBoolEventChannelTransmitter : IEventChannelTransmitter<bool> { }
	
	[Serializable]
	public class IBoolEventChannelListenerContainer : IUnifiedContainer<IBoolEventChannelListener> { }
	
	[Serializable]
	public class IBoolEventChannelTransmitterContainer : IUnifiedContainer<IBoolEventChannelTransmitter> { }
}