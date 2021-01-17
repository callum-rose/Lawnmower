using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(VoidEventChannel), menuName = SONames.CoreDir + nameof(VoidEventChannel))]
	public class VoidEventChannel : BaseEventChannel
	{
		protected override bool ShouldBeSolo => false;
	}
}