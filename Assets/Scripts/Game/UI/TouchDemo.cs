#if UNITY_EDITOR

using BalsamicBits.Extensions;
using Lean.Touch;
using Sirenix.Utilities;
using UnityEngine;

namespace Game.UI
{
    public class TouchDemo : MonoBehaviour
    {
        [SerializeField] private float size;
        [SerializeField] private Texture texture;

        private void OnGUI()
        {
            foreach (LeanFinger finger in LeanTouch.Fingers)
            {
                Rect rect = new Rect().SetSize(size, size).SetCenter(finger.ScreenPosition.SetY(Screen.height - finger.ScreenPosition.y));
                GUI.DrawTexture(rect, texture);
            }
        }
    }
}
#endif