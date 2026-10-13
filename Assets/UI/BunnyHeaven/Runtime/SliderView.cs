using System;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>Hand-drawn 0..1 slider (Music / Sounds volume).</summary>
    [RequireComponent(typeof(Slider))]
    public class SliderView : MonoBehaviour
    {
        private Slider _slider;

        public event Action<float> ValueChanged;

        public Slider Slider => _slider != null ? _slider : _slider = GetComponent<Slider>();

        public float Value
        {
            get => Slider.value;
            set => Slider.value = value;
        }

        private void Awake() => Slider.onValueChanged.AddListener(HandleChanged);

        private void HandleChanged(float value) => ValueChanged?.Invoke(value);

        public void SetValueWithoutNotify(float value) => Slider.SetValueWithoutNotify(value);
    }
}
