using UnityEngine;

namespace Core
{
	[CreateAssetMenu(fileName = nameof(Vector3AndIntEventChannel), menuName = SONames.CoreDir + nameof(Vector3AndIntEventChannel))]
	public class Vector3AndIntEventChannel : BaseEventChannel<Vector3, int>
	{
	}
}