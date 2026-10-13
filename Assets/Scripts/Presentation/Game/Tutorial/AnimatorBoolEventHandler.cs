using UnityEngine;

namespace Game.Tutorial
{
	internal class AnimatorBoolEventHandler : AnimatorEventHandlerBase
	{
		protected override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Bool;

		public void Set(bool value)
		{
			Animator.SetBool(NameHash, value);
		}
	}
}