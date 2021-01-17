using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(Vector3EventChannel), menuName = SONames.CoreDir + nameof(Vector3EventChannel))]
	public class Vector3EventChannel : BaseEventChannel<Vector3>
	{
		protected override bool ShouldBeSolo => false;
	}
}