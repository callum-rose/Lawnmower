using Lean.Touch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Mowers.Input
{
    [ExecuteInEditMode]
	internal class LeanTouchInitialiser : MonoBehaviour
	{
        #region Unity

        private void Start()
        {
            LeanTouch.Instance.ReferenceDpi = (int)Screen.dpi;
        }

        #endregion

        #region API

        #endregion

        #region Events

        #endregion

        #region Methods

        #endregion
    }
}
