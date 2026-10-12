using System.Collections.Generic;
using BalsamicBits.Extensions;
using Core;
using UnityEngine;

namespace UI.Buttons
{
	[System.Serializable]
	internal struct IconSprite
	{
		public IconType icon;
		public Sprite sprite;
	}

	[CreateAssetMenu(fileName = nameof(IconDataHolder), menuName = SoNames.UIDir + nameof(IconDataHolder))]
	internal class IconDataHolder : ScriptableObject
	{
		[SerializeField] private List<IconSprite> icons = new List<IconSprite>();

		#region Unity

#if UNITY_EDITOR
		private void OnValidate()
		{
			// Back-fill an entry for every icon type so new enum values show up in the inspector.
			foreach (IconType i in EnumExtensions.GetValues<IconType>())
			{
				if (!icons.Exists(e => e.icon == i))
				{
					icons.Add(new IconSprite { icon = i });
				}
			}
		}
#endif

		#endregion

		#region API

		public Sprite GetIcon(IconType icon)
		{
			foreach (IconSprite entry in icons)
			{
				if (entry.icon == icon)
				{
					return entry.sprite;
				}
			}

			return null;
		}

		#endregion
	}
}
