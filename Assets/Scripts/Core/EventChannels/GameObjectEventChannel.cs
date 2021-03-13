using System;
using IUnified;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(GameObjectEventChannel), menuName = SoNames.CoreDir + nameof(GameObjectEventChannel))]
	public class GameObjectEventChannel : BaseEventChannel<GameObject>, IGameObjectEventChannelListener, IGameObjectEventChannelTransmitter
	{
		protected override bool ShouldBeSolo => false;
	}
	
	public interface IGameObjectEventChannelListener : IEventChannelListener<GameObject> { }
	
	public interface IGameObjectEventChannelTransmitter : IEventChannelTransmitter<GameObject> { }
	
	[Serializable]
	public class IGameObjectEventChannelListenerContainer : IUnifiedContainer<IGameObjectEventChannelListener> { }
	
	[Serializable]
	public class IGameObjectEventChannelTransmitterContainer : IUnifiedContainer<IGameObjectEventChannelTransmitter> { }
}