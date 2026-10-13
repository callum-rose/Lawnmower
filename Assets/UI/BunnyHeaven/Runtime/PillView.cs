using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>Counter pill (e.g. "LEVEL 7", "12/20"): a white pill with a coloured icon chip on the left.</summary>
    public class PillView : MonoBehaviour
    {
        [SerializeField] private FrameView frame;
        [SerializeField] private ThemedGraphic chip;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;

        public TMP_Text Label => label;

        public void SetText(string text) => label.text = text;

        public void SetIcon(Sprite sprite)
        {
            icon.sprite = sprite;
            chip.gameObject.SetActive(sprite != null);
        }

        public void SetChipColor(ThemeColor token) => chip.Token = token;

        /// <summary>Shadow colour should match the surface the pill sits on (YellowShadow on menus, TealShadow in game).</summary>
        public void SetShadow(ThemeColor token) => frame.SetShadow(token);
    }
}
