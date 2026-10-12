using System;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>On/off switch (e.g. Haptics). Tapping flips it; the knob slides across.</summary>
    [RequireComponent(typeof(Button))]
    public class ToggleSwitchView : MonoBehaviour
    {
        [SerializeField] private bool isOn = true;
        [SerializeField] private ThemedGraphic track;
        [SerializeField] private RectTransform knob;
        [SerializeField] private ThemeColor onColor = ThemeColor.Green;
        [SerializeField] private ThemeColor offColor = ThemeColor.SliderTrack;
        [SerializeField] private float slideTime = 0.12f;

        private Coroutine _slide;

        public event Action<bool> ValueChanged;

        public bool IsOn => isOn;

        private void Awake() => GetComponent<Button>().onClick.AddListener(Toggle);

        private void OnEnable() => Refresh(false);

        public void Toggle() => SetIsOn(!isOn);

        public void SetIsOn(bool value)
        {
            if (value == isOn)
                return;

            isOn = value;
            Refresh(true);
            ValueChanged?.Invoke(isOn);
        }

        public void SetIsOnWithoutNotify(bool value)
        {
            isOn = value;
            Refresh(false);
        }

        private void Refresh(bool animate)
        {
            if (track != null)
                track.Token = isOn ? onColor : offColor;
            if (knob == null)
                return;

            var from = knob.anchorMin.x;
            var to = isOn ? 1f : 0f;
            if (_slide != null)
                StopCoroutine(_slide);
            if (!animate)
            {
                SetKnob(to);
                return;
            }

            _slide = UiTween.Run(this, slideTime, t => SetKnob(Mathf.Lerp(from, to, UiTween.EaseOutQuad(t))));
        }

        private void SetKnob(float x)
        {
            knob.anchorMin = new Vector2(x, 0.5f);
            knob.anchorMax = new Vector2(x, 0.5f);
            knob.pivot = new Vector2(x, 0.5f);
            knob.anchoredPosition = Vector2.zero;
        }
    }
}
