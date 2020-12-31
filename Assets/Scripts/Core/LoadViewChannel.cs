using UnityEngine;

namespace Core
{
	[CreateAssetMenu(fileName = nameof(LoadViewChannel),
		menuName = SONames.CoreDir + nameof(LoadViewChannel))]
	public class LoadViewChannel : BaseEvent
	{
		[SerializeField] private UnityScene sceneToLoad;

		protected override void Raise()
		{
			ViewManager.Instance.Load(sceneToLoad);
		}
	}
}