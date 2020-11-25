using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Buttons
{
	internal class ButtonIconSetter : MonoBehaviour
	{
		[SerializeField] private IconDataHolder iconData;
		[SerializeField] private IconType icon;
		[SerializeField] private Image iconImage;

        #region Unity

        private void OnValidate()
        {
            UpdateSprite();
        }

        #endregion

        #region API

        public void Set(IconType icon)
        {
            this.icon = icon;
            UpdateSprite();
        }

        #endregion

        #region Methods

        private void UpdateSprite()
        {
            iconImage.sprite = iconData.GetIcon(icon);
        }

        #endregion
    }
}