using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>
    /// Passive view for the kit's buttons (BigButton, RoundIconButton). Raises <see cref="Clicked"/>;
    /// <see cref="AnyClicked"/> lets a single listener (e.g. the click sound) hear every kit button.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonView : MonoBehaviour
    {
        [SerializeField] private ButtonColor color = ButtonColor.Green;
        [SerializeField] private FrameView frame;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [Tooltip("Tint the label with the button's content colour. Off for labels drawn outside the button.")]
        [SerializeField] private bool tintLabel = true;

        private Button _button;

        public static event Action<ButtonView> AnyClicked;

        public event Action Clicked;

        public Button Button => _button != null ? _button : _button = GetComponent<Button>();
        public ButtonColor Color => color;
        public TMP_Text Label => label;
        public Image Icon => icon;

        public bool Interactable
        {
            get => Button.interactable;
            set => Button.interactable = value;
        }

        private void Awake() => Button.onClick.AddListener(HandleClick);

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClick);
        }

        private void OnValidate() => ApplyColor();

        private void HandleClick()
        {
            Clicked?.Invoke();
            AnyClicked?.Invoke(this);
        }

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text;
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon == null)
                return;

            icon.sprite = sprite;
            icon.gameObject.SetActive(sprite != null);
        }

        public void SetColor(ButtonColor value)
        {
            color = value;
            ApplyColor();
        }

        private void ApplyColor()
        {
            var palette = UiTheme.GetPalette(color);
            if (frame != null)
                frame.SetColors(palette.Fill, palette.Shadow);
            SetContentToken(icon, palette.Content);
            if (tintLabel)
                SetContentToken(label, palette.Content);
        }

        private static void SetContentToken(Component graphic, ThemeColor token)
        {
            if (graphic != null && graphic.TryGetComponent(out ThemedGraphic themed))
                themed.Token = token;
        }
    }
}
