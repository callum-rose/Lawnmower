using Core;
using UnityEngine;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(MowerMoverData), menuName = SoNames.GameDir + nameof(MowerMoverData))]
	public class MowerMoverData : ScriptableObject
	{
		[SerializeField, Min(0)] private float acceleration = 1;
		[SerializeField, Range(0, 1)] private float friction = 0.9f;

		public float Acceleration => acceleration;

		public float Friction => friction;
	}
}
