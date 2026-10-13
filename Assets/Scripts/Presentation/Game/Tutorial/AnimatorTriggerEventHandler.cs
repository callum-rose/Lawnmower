
using UnityEngine;

namespace Game.Tutorial
{
	internal class AnimatorTriggerEventHandler : AnimatorEventHandlerBase
	{
		protected override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Trigger;
		
		public void Trigger()
		{
			Animator.SetTrigger(NameHash);
		}
	}
}