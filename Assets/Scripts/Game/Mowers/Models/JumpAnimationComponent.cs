using System;
using System.Linq;
using Game.Core;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Mowers.Models
{
	internal class JumpAnimationComponent : MonoBehaviour
	{
		[SerializeField] private MowerMovementManager mowerMovementManager;
		[SerializeField] private Animator animator;
		[SerializeField] private AnimationCurve triggerGapToJumpSpeed;
		[SerializeField] private AnimationSpeedHandler animationSpeedHandler;
		[SerializeField] private UnityEvent jumpAction;

		private static readonly int jumpSpeedHash = Animator.StringToHash("JumpSpeed");

		private float _slowestJumpDuration;

		public void Awake()
		{
			float maxTimeKey = triggerGapToJumpSpeed.keys.Max(k => k.time);
			_slowestJumpDuration = 1f / triggerGapToJumpSpeed.Evaluate(maxTimeKey);
		}

		private void OnEnable()
		{
			mowerMovementManager.Moved += MowerMovementManagerOnMoved;
		}

		private void OnDisable()
		{
			mowerMovementManager.Moved -= MowerMovementManagerOnMoved;
		}

		private void Update()
		{
			if (animationSpeedHandler.LastJumpTimeSpacing > _slowestJumpDuration)
			{
				return;
			}

			float jumpSpeed = triggerGapToJumpSpeed.Evaluate(animationSpeedHandler.JumpTimeSpacing);
			animator.SetFloat(jumpSpeedHash, jumpSpeed);
		}

		private void MowerMovementManagerOnMoved(GridVector from, GridVector to, Xor inverted)
		{
			Animate();
		}

		[Button("Debug Animate", Expanded = true)]
		private void Animate()
		{
			jumpAction.Invoke();
		}
	}
}