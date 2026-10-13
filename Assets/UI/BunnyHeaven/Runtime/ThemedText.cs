using TMPro;
using UnityEngine;

namespace BunnyHeaven.UI
{
    /// <summary>Applies the theme's font (and, for headings, the optional ink-shadow material) to a TMP text.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public class ThemedText : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private ThemeFont font = ThemeFont.Heading;
        [SerializeField] private bool inkShadow;

        public UiTheme Theme
        {
            get => theme;
            set { theme = value; Apply(); }
        }

        public ThemeFont Font
        {
            get => font;
            set { font = value; Apply(); }
        }

        public bool InkShadow
        {
            get => inkShadow;
            set { inkShadow = value; Apply(); }
        }

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void Apply()
        {
            if (theme == null)
                return;

            var text = GetComponent<TMP_Text>();
            var asset = theme.GetFont(font);
            if (asset == null)
                return;

            text.font = asset;
            text.fontSharedMaterial = inkShadow && font == ThemeFont.Heading && theme.headingShadowMaterial != null
                ? theme.headingShadowMaterial
                : asset.material;
        }
    }
}
