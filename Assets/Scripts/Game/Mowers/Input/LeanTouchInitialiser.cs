using Lean.Touch;
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
