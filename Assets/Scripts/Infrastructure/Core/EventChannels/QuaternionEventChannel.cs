using System;
using IUnified;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(QuaternionEventChannel), menuName = SoNames.CoreDir + nameof(QuaternionEventChannel))]
	public class QuaternionEventChannel : BaseEventChannel<Quaternion>, IQuaternionEventChannelListener, IQuaternionEventChannelTransmitter
	{
		protected override bool ShouldBeSolo => false;
	}
	
	public interface IQuaternionEventChannelListener : IEventChannelListener<Quaternion> { }
	
	public interface IQuaternionEventChannelTransmitter : IEventChannelTransmitter<Quaternion> { }
	
	[Serializable]
	public class IQuaternionEventChannelListenerContainer : IUnifiedContainer<IQuaternionEventChannelListener> { }
	
	[Serializable]
	public class IQuaternionEventChannelTransmitterContainer : IUnifiedContainer<IQuaternionEventChannelTransmitter> { }
}