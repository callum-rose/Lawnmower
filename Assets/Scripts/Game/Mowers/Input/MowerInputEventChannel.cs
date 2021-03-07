using Core;
using Core.EventChannels;
using Game.Core;
using UnityEngine;

namespace Game.Mowers.Input
{
	[CreateAssetMenu(fileName = nameof(MowerInputEventChannel), menuName = SONames.GameDir + nameof(MowerInputEventChannel))]
	public class MowerInputEventChannel : BaseEventChannel<GridVector>
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
}