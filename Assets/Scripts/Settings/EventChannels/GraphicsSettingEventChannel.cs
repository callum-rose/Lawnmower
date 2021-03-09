using System;
using Core;
using Core.EventChannels;
using IUnified;
using UnityEngine;

namespace Settings.EventChannels
{
	[CreateAssetMenu(fileName = nameof(GraphicsSettingEventChannel), menuName = SONames.CoreDir + nameof(GraphicsSettingEventChannel))]
	public class GraphicsSettingEventChannel : BaseEventChannel<GraphicsSetting>, IGraphicsSettingEventChannelListener, IGraphicsSettingEventChannelTransmitter
	{
		protected override bool ShouldBeSolo => true;
	}
	
	public interface IGraphicsSettingEventChannelListener : IEventChannelListener<GraphicsSetting> { }
	
	public interface IGraphicsSettingEventChannelTransmitter : IEventChannelTransmitter<GraphicsSetting> { }
	
	[Serializable]
	public class IGraphicsSettingEventChannelListenerContainer : IUnifiedContainer<IGraphicsSettingEventChannelListener> { }
	
	[Serializable]
	public class IGraphicsSettingEventChannelTransmitterContainer : IUnifiedContainer<IGraphicsSettingEventChannelTransmitter> { }
}