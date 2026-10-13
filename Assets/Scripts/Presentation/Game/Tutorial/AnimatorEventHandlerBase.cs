using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Tutorial
{
	internal abstract class AnimatorEventHandlerBase : MonoBehaviour
	{
		[SerializeField] private Animator animator;
		
		[SerializeField]
		private string propertyName;
		
		protected Animator Animator => animator;

		protected abstract AnimatorControllerParameterType Type { get; }
		
		protected int NameHash { get; private set; }

		private IEnumerable<string> AnimatorParameterNames => Animator
			.parameters
			.Where(p => p.type == Type)
			.Select(p => p.name);

		protected virtual void Awake()
		{
			SetNameHash();
		}

		private void Reset()
		{
			propertyName = AnimatorParameterNames.First();
			SetNameHash();
		}

		protected virtual void OnValidate()
		{
			SetNameHash();
		}

		private void SetNameHash()
		{
			NameHash = Animator.StringToHash(propertyName);
		}
	}
}