using Game.Core;
using UnityEngine;

namespace Game.Mowers
{
	internal class MowerObjectCreator : MonoBehaviour
	{
		[SerializeField] private Transform container;
		[SerializeField] private Positioner positioner;

		public MowerObject Create(MowerData mower, IMowerPosition mowerPosition)
		{
			MowerObject newMower = Instantiate(mower.Prefab, container);
			newMower.Init(positioner);
			newMower.Bind(mowerPosition);

			return newMower;
		}
	}
}