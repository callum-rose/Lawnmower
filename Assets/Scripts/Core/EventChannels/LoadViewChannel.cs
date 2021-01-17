using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(LoadViewChannel),
		menuName = SONames.CoreDir + nameof(LoadViewChannel))]
	public class LoadViewChannel : BaseEventChannel
	{
		[SerializeField] private UnityScene sceneToLoad;

		protected override bool ShouldBeSolo => true;

		private void Awake()
		{
			EventRaised += LoadScene;
		}
		
		private void OnDestroy()
		{
			EventRaised -= LoadScene;
		}

		private void LoadScene()
		{
			ViewManager.Instance.Load(sceneToLoad);
		}
	}
}