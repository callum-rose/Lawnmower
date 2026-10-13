using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>
    /// Settings-style row: icon chip, label, and a trailing slot. The slot holds a chevron by default;
    /// parent a ToggleSegment / ToggleSwitch / Slider into <see cref="Accessory"/> for control rows.
    /// </summary>
    public class ListRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private ThemedGraphic chip;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private ThemedGraphic labelColor;
        [SerializeField] private RectTransform accessory;
        [SerializeField] private ThemedGraphic chevron;

        public event Action Clicked;

        public RectTransform Accessory => accessory;
        public TMP_Text Label => label;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void SetLabel(string text) => label.text = text;

        public void SetIcon(Sprite sprite) => icon.sprite = sprite;

        public void SetChipColor(ThemeColor token) => chip.Token = token;

        /// <summary>Ink for normal rows, Red for destructive rows like Reset Progress. Tints the chevron too.</summary>
        public void SetTextColor(ThemeColor token)
        {
            labelColor.Token = token;
            if (chevron != null)
                chevron.Token = token;
        }

        /// <summary>Chevron rows are tappable; control rows hide the chevron and stop acting as a button.</summary>
        public void SetShowChevron(bool show)
        {
            if (chevron != null)
                chevron.gameObject.SetActive(show);
            if (button != null)
                button.enabled = show;
        }
    }
}
