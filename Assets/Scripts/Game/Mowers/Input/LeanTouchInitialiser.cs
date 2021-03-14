using Lean.Touch;
using UnityEngine;

namespace Game.Mowers.Input
{
    [ExecuteInEditMode]
	internal class LeanTouchInitialiser : MonoBehaviour
	{
        private void OnEnable()
        {
            LeanTouch.Instance.ReferenceDpi = (int)Screen.dpi;
        }
    }
}
