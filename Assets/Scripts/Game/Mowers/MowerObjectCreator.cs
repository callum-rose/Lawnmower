using Game.Core;
using UnityEngine;

namespace Game.Mowers
{
	internal class MowerObjectCreator : MonoBehaviour
	{
		[SerializeField] private Transform container;
		[SerializeField] private Positioner positioner;

		public GameObject Create(MowerData mower, MowerMoverr mowerMoverr)
		{
			MowerObject newMower = Instantiate(mower.Prefab, container);
			newMower.Init(positioner);
			newMower.Bind(mowerMoverr);

			return newMower.gameObject;
		}
	}
}