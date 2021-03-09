using System;
using Core;
using Core.EventChannels;
using Game.Core;
using IUnified;
using UnityEngine;

namespace Game.Mowers.Input
{
	[CreateAssetMenu(fileName = nameof(MowerInputEventChannel), menuName = SONames.GameDir + nameof(MowerInputEventChannel))]
	public class MowerInputEventChannel : BaseEventChannel<GridVector>, IMowerInputEventChannelListener, IMowerInputEventChannelTransmitter
	{
		public bool IsBlocked { get; set; }
		
		protected override bool ShouldBeSolo => true;

		public override void Raise(GridVector vector)
		{
			if (IsBlocked)
			{
				return;
			}
			
			base.Raise(vector);
		}
	}
	
	public interface IMowerInputEventChannelListener : IEventChannelListener<GridVector> { }
	
	public interface IMowerInputEventChannelTransmitter : IEventChannelTransmitter<GridVector> { }
	
	[Serializable]
	public class IMowerInputEventChannelListenerContainer : IUnifiedContainer<IMowerInputEventChannelListener> { }
	
	[Serializable]
	public class IMowerInputEventChannelTransmitterContainer : IUnifiedContainer<IMowerInputEventChannelTransmitter> { }
}