using Game.Core;
using Game.Mowers.Input;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Mowers.Models
{
	internal class AnimationSpeedHandler : MonoBehaviour, INeedMowerPosition
	{
		[SerializeField, Min(0.1f)] private float minAnimDuration = 0.1f;
		[SerializeField, Min(0.1f)] private float maxAnimDuration = 1;

		public float RecommendedAnimDuration => Mathf.Clamp(JumpTimeSpacing, minAnimDuration, maxAnimDuration);
		public float RecommendedAnimSpeed => 1 / RecommendedAnimDuration;

		public float JumpTimeSpacing {
			get
			{
				if (LastJumpTimeSpacing > maxAnimDuration)
				{
					return maxAnimDuration;
				}
				
				return Time.time - _lastJumpTime;
			}
		}
		public float LastJumpTimeSpacing => _lastJumpTime - _lastLastJumpTime;
		
		private float _lastJumpTime;
		private float _lastLastJumpTime;
		private IMowerPosition _mowerPosition;

		void INeedMowerPosition.Set(IMowerPosition mowerPosition)
		{
			_mowerPosition = mowerPosition;
			_mowerPosition.CurrentPosition.ValueChangedFromTo += MowerMovementManagerOnMoved;
		}

		void INeedMowerPosition.Clear()
		{
			_mowerPosition.CurrentPosition.ValueChangedFromTo -= MowerMovementManagerOnMoved;
			_mowerPosition = null;
		}

		private void MowerMovementManagerOnMoved(GridVector from, GridVector to, Xor inverted)
		{
			_lastLastJumpTime = _lastJumpTime;
			_lastJumpTime = Time.time;		
		}
	}
}