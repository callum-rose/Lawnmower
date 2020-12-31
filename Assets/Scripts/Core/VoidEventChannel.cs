using System;
using UnityEngine;

namespace Core
{
	[CreateAssetMenu(fileName = nameof(VoidEventChannel), menuName = SONames.CoreDir + nameof(VoidEventChannel))]
	public class VoidEventChannel : BaseEventChannel
	{
		public event Action EventRaised;

		protected override Delegate EventDelegate => EventRaised;
	}
}