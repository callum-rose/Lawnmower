using Game.Core;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(TileObjectEventChannel), menuName = SONames.GameDir + nameof(TileObjectEventChannel))]
	public class TileObjectEventChannel : BaseEventChannel<GameObject, GridVector>
	{
		protected override bool ShouldBeSolo => false;
	}
}