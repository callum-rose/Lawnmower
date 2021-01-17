using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(GameObjectEventChannel), menuName = SONames.CoreDir + nameof(GameObjectEventChannel))]
	public class GameObjectEventChannel : BaseEventChannel<GameObject>
	{
		protected override bool ShouldBeSolo => false;
	}
}