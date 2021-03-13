using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(Vector3AndIntEventChannel), menuName = SoNames.CoreDir + nameof(Vector3AndIntEventChannel))]
	public class Vector3AndIntEventChannel : BaseEventChannel<Vector3, int>
	{
		protected override bool ShouldBeSolo => false;
	}
}