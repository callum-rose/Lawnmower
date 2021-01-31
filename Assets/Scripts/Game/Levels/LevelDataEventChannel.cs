using Core;
using Core.EventChannels;
using UnityEngine;

namespace Game.Levels
{
	[CreateAssetMenu(fileName = nameof(LevelDataEventChannel), menuName = SONames.GameDir + nameof(LevelDataEventChannel))]
	internal sealed class LevelDataEventChannel : BaseEventChannel<IReadOnlyLevelData>
	{
		protected override bool ShouldBeSolo => true;
	}
}