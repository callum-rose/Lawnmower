using UnityEngine;

namespace Game.Core
{
	public class PostProcessRemover : MonoBehaviour
	{
		[SerializeField] private Component[] componentsToRemove;

		public void RemoveAllThenSelf()
		{
			foreach (var c in componentsToRemove)
			{
				DestroyImmediate(c);
			}

			DestroyImmediate(this);
		}
	}
}