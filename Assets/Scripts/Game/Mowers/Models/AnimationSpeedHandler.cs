using Game.Core;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Mowers.Models
{
	internal class AnimationSpeedHandler : MonoBehaviour
	{
		[SerializeField] private MowerMovementManager mowerMovementManager;
		
		public float JumpTimeSpacing => Time.time - _lastJumpTime;
		public float LastJumpTimeSpacing => _lastJumpTime - _lastLastJumpTime;
		
		private float _lastJumpTime;
		private float _lastLastJumpTime;

		private void OnEnable()
		{
			mowerMovementManager.Moved += MowerMovementManagerOnMoved;
		}

		private void OnDisable()
		{
			mowerMovementManager.Moved -= MowerMovementManagerOnMoved;
		}

		private void MowerMovementManagerOnMoved(GridVector from, GridVector to, Xor inverted)
		{
			_lastLastJumpTime = _lastJumpTime;
			_lastJumpTime = Time.time;		
		}
	}
}