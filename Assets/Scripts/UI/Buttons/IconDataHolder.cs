using BalsamicBits.Extensions;
using Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
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
	internal class IconDataHolder : SerializedScriptableObject
	{
		[SerializeField] private Dictionary<IconType, Sprite> iconToSpriteDict;

		// Unity-serialized replacement for the Odin dictionary (populated by the Odin data migration).
		[SerializeField] internal List<IconSprite> icons = new List<IconSprite>();

		internal Dictionary<IconType, Sprite> LegacyIcons => iconToSpriteDict;

        #region Unity

        private void Awake()
        {
            if (iconToSpriteDict == null || iconToSpriteDict.Count == 0)
            {
                iconToSpriteDict = EnumExtensions.GetValues<IconType>().ToDictionary(i => i, i => (Sprite)null);
            }

            foreach (IconType i in EnumExtensions.GetValues<IconType>())
            {
                if (!iconToSpriteDict.ContainsKey(i))
                {
                    iconToSpriteDict.Add(i, null);
                }
            }
        }

        #endregion

        #region API

        public Sprite GetIcon(IconType icon)
        {
            return iconToSpriteDict[icon];
        }

        #endregion

        #region Events

        #endregion

        #region Methods

        #endregion
    }
}