using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(CloseDialogEventChannel),
		menuName = SoNames.CoreDir + nameof(CloseDialogEventChannel))]
	public class CloseDialogEventChannel : BaseEventChannel<int>
	{
		protected override bool ShouldBeSolo => true;
	}
}