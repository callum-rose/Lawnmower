using UnityEngine;

namespace Core
{
	[CreateAssetMenu(fileName = nameof(LoadViewChannel),
		menuName = SONames.CoreDir + nameof(LoadViewChannel))]
	public class LoadViewChannel : BaseEventChannel
	{
		[SerializeField] private UnityScene sceneToLoad;

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