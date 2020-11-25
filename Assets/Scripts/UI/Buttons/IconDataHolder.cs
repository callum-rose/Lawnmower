using BalsamicBits.Extensions;
using Core;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI.Buttons
{
    [CreateAssetMenu(fileName = nameof(IconDataHolder), menuName = SONames.UIDir + nameof(IconDataHolder))]
	internal class IconDataHolder : SerializedScriptableObject
	{
		[SerializeField] private Dictionary<IconType, Sprite> iconToSpriteDict;

        #region Unity

        private void Awake()
        {
            if (iconToSpriteDict == null || iconToSpriteDict.Count == 0)
            {
                iconToSpriteDict = EnumExtensions.GetValues<IconType>().ToDictionary(i => i, i => (Sprite)null);
            }

            foreach (var i in EnumExtensions.GetValues<IconType>())
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