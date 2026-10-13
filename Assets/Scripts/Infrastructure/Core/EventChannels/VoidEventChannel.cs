using System;
using IUnified;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(VoidEventChannel), menuName = SoNames.CoreDir + nameof(VoidEventChannel))]
	public class VoidEventChannel : BaseEventChannel, IVoidEventChannelListener, IVoidEventChannelTransmitter
	{
		[SerializeField] private bool shouldBeSolo;

		protected override bool ShouldBeSolo => shouldBeSolo;
	}
	
	public interface IVoidEventChannelListener : IEventChannelListener { }
	public interface IVoidEventChannelTransmitter : IEventChannelTransmitter { }
	
	[Serializable]
	public class IVoidEventChannelListenerContainer : IUnifiedContainer<IVoidEventChannelListener> { }	
	
	[Serializable]
	public class IVoidEventChannelTransmitterContainer : IUnifiedContainer<IVoidEventChannelTransmitter> { }
}