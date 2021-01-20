using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(LoadViewChannel),
		menuName = SONames.CoreDir + nameof(LoadViewChannel))]
	public class LoadViewChannel : BaseEventChannel
	{
		[SerializeField] private UnityScene sceneToLoad;

		protected override bool ShouldBeSolo => false;

		private void OnEnable()
		{
			EventRaised += LoadScene;
		}
		
		private void OnDisable()
		{
			EventRaised -= LoadScene;
		}

		private void LoadScene()
		{
			ViewManager.Instance.Load(sceneToLoad);
		}
	}
}