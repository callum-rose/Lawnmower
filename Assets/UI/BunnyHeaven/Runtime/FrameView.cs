using UnityEngine;

namespace BunnyHeaven.UI
{
    /// <summary>A hand-drawn frame: a tinted body (with baked ink outline) over an offset shadow.</summary>
    public class FrameView : MonoBehaviour
    {
        [SerializeField] private ThemedGraphic fill;
        [SerializeField] private ThemedGraphic shadow;

        public ThemedGraphic Fill => fill;
        public ThemedGraphic Shadow => shadow;

        public void SetColors(ThemeColor fillColor, ThemeColor shadowColor)
        {
            if (fill != null)
                fill.Token = fillColor;
            if (shadow != null)
                shadow.Token = shadowColor;
        }

        public void SetFill(ThemeColor fillColor)
        {
            if (fill != null)
                fill.Token = fillColor;
        }

        public void SetShadow(ThemeColor shadowColor)
        {
            if (shadow != null)
                shadow.Token = shadowColor;
        }
    }
}
