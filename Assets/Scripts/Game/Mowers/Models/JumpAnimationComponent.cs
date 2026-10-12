using Game.Core;
using Game.Mowers.Input;
using Game.UndoSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Mowers.Models
{
	internal class JumpAnimationComponent : MonoBehaviour, INeedMowerPosition
	{
		[SerializeField] private Animator animator;
		[SerializeField] private AnimationSpeedHandler animationSpeedHandler;
		[SerializeField] private UnityEvent jumpAction;

		private static readonly int jumpSpeedHash = Animator.StringToHash("JumpSpeed");

		private IMowerPosition _mowerPosition;
		
		#region Unity

		private void Update()
		{
			animator.SetFloat(jumpSpeedHash, animationSpeedHandler.RecommendedAnimSpeed);
		}
		
		#endregion

		#region API

		void INeedMowerPosition.Set(IMowerPosition mowerPosition)
		{
			_mowerPosition = mowerPosition;
			_mowerPosition.CurrentPosition.ValueChangedFromTo += OnMowerMoved;
		}
		
		void INeedMowerPosition.Clear()
		{
			_mowerPosition.CurrentPosition.ValueChangedFromTo -= OnMowerMoved;
			_mowerPosition = null;
		}

		#endregion
		
		#region Events

		private void OnMowerMoved(GridVector from, GridVector to, Xor inverted)
		{
			if (inverted)
			{
				return;
			}
			
			Animate();
		}
		
		#endregion

		#region Methods
		
		[ContextMenu("Debug Animate")]
		private void Animate()
		{
			jumpAction.Invoke();
		}
		
		#endregion
	}
}