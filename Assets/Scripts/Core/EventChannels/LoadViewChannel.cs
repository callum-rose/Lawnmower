using System;
using System.Collections.Generic;
using System.Linq;
using BalsamicBits.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.EventChannels
{
	[CreateAssetMenu(fileName = nameof(LoadViewChannel),
		menuName = SoNames.CoreDir + nameof(LoadViewChannel))]
	public class LoadViewChannel : BaseEventChannel
	{
		[SerializeField, ValueDropdown(nameof(SceneNames), HideChildProperties = true), Required]
		private string sceneToLoad;
		
		protected override bool ShouldBeSolo => false;

		private IEnumerable<string> SceneNames => EnumExtensions.GetValues<UnityScene>().Select(e => e.ToString());
		[ShowInInspector] private UnityScene Scene => (UnityScene) Enum.Parse(typeof(UnityScene), sceneToLoad);
		
		private void OnEnable()
		{
			EventRaised += LoadScene;
		}

		private void OnDisable()
		{
			EventRaised -= LoadScene;
		}

		private void LoadScene()
		{
			ViewManager.Instance.Load(Scene);
		}
	}
}