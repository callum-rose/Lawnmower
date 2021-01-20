using Game.Core;
using UnityEngine;

namespace Game.Mowers
{
	internal class MowerObjectCreator : MonoBehaviour
	{
		[SerializeField] private Transform container;
		[SerializeField] private Positioner positioner;

		public MowerObject Create(MowerData mower, MowerMover mowerMover)
		{
			MowerObject newMower = Instantiate(mower.Prefab, container);
			newMower.Init(positioner);
			newMower.Bind(mowerMover);

			return newMower;
		}
	}
}