using System;
using IUnified;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(Vector3EventChannel), menuName = SONames.CoreDir + nameof(Vector3EventChannel))]
	public class Vector3EventChannel : BaseEventChannel<Vector3>, IVector3EventChannelListener, IVector3EventChannelTransmitter
	{
		protected override bool ShouldBeSolo => false;
	}
	
	public interface IVector3EventChannelListener : IEventChannelListener<Vector3> { }
	
	public interface IVector3EventChannelTransmitter : IEventChannelTransmitter<Vector3> { }
	
	[Serializable]
	public class IVector3EventChannelListenerContainer : IUnifiedContainer<IVector3EventChannelListener> { }
	
	[Serializable]
	public class IVector3EventChannelTransmitterContainer : IUnifiedContainer<IVector3EventChannelTransmitter> { }
}