using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.EventChannels
{
	public abstract class UnreferencedScriptableObject : ScriptableObject
	{
		[SerializeField] private bool enableInAllScenes;
		[SerializeField, HideIf(nameof(enableInAllScenes))] private UnityScene[] scenesToBeEnabledIn;

		public bool EnableInAllScenes => enableInAllScenes;
		public IEnumerable<UnityScene> ScenesToBeEnabledIn => scenesToBeEnabledIn;
	}
}