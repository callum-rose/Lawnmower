using Core;
using Core.EventChannels;
using Game.Core;
using UnityEngine;

namespace Game.Mowers.Input
{
	[CreateAssetMenu(fileName = nameof(MowerInputEventChannel), menuName = SONames.GameDir + nameof(MowerInputEventChannel))]
	public class MowerInputEventChannel : BaseEventChannel<GridVector>
	{
		protected override bool ShouldBeSolo => true;
	}
}