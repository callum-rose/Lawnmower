using Game.UndoSystem;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(UndoableEventChannel), menuName = SoNames.CoreDir + nameof(UndoableEventChannel))]
	public class UndoableEventChannel : BaseEventChannel<Xor>
	{
		protected override bool ShouldBeSolo => false;
	}
}