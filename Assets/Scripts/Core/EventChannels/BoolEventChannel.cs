using System;
using IUnified;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(BoolEventChannel), menuName = SoNames.CoreDir + nameof(BoolEventChannel))]
	public class BoolEventChannel : BaseEventChannel<bool>, IBoolEventChannelListener, IBoolEventChannelTransmitter
	{
		[SerializeField] private bool pushLastDataOnSubscribe = true;
		
		protected override bool ShouldBeSolo => true;
		protected override bool PushLastDataOnSubscribe => pushLastDataOnSubscribe;
	}
	
	public interface IBoolEventChannelListener : IEventChannelListener<bool> { }
	
	public interface IBoolEventChannelTransmitter : IEventChannelTransmitter<bool> { }
	
	[Serializable]
	public class IBoolEventChannelListenerContainer : IUnifiedContainer<IBoolEventChannelListener> { }
	
	[Serializable]
	public class IBoolEventChannelTransmitterContainer : IUnifiedContainer<IBoolEventChannelTransmitter> { }
}