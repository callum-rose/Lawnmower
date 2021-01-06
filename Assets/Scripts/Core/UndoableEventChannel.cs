using Game.UndoSystem;
using UnityEngine;

namespace Core
{
	[CreateAssetMenu(fileName = nameof(UndoableEventChannel), menuName = SONames.CoreDir + nameof(UndoableEventChannel))]
	public class UndoableEventChannel : BaseEventChannel<Xor>
	{

	}
}