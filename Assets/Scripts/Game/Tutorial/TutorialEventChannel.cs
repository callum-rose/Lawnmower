using Core;
using Core.EventChannels;
using UnityEngine;

namespace Game.Tutorial
{
	[CreateAssetMenu(fileName = nameof(TutorialEventChannel), menuName = SoNames.GameDir + nameof(TutorialEventChannel))]
	internal class TutorialEventChannel : BaseEventChannel<TutorialStage>
	{
		[SerializeField] private TutorialStage stage;

		protected override bool ShouldBeSolo => false;

		public void Raise()
		{
			Raise(stage);
		}
	}
}