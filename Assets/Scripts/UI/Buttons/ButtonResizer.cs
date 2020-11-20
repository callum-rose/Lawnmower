using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BalsamicBits.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.UI;

namespace UI.Buttons
{
    internal class ButtonResizer : MonoBehaviour
    {
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private Ease animationEase = Ease.InOutCubic;
        [SerializeField] private HorizontalLayoutGroup layoutGroup;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Image icon;

        private RectTransform RectTransform => transform as RectTransform;

        private Tween _currentTween;

        [Button]
        public void SetWidth(float width, bool instant = false)
        {
#if UNITY_EDITOR
            if (instant || !Application.isPlaying)
            {
                RectTransform.sizeDelta = RectTransform.sizeDelta.SetX(width);
                return;
            }
#endif

            if (_currentTween?.IsComplete() ?? false)
            {
                _currentTween.Kill();
            }

            Vector2 currentSize = RectTransform.sizeDelta;
            _currentTween = RectTransform.DOSizeDelta(currentSize.SetX(width), animationDuration).SetEase(animationEase);
        }

        [Button]
        public void FitToContent(bool instant = false)
        {
            float fittedWidth = layoutGroup.padding.left +
                (icon.gameObject.activeSelf ? icon.rectTransform.sizeDelta.x : 0) +
                (icon.gameObject.activeSelf && text.gameObject.activeSelf ? layoutGroup.spacing : 0) +
                (text.gameObject.activeSelf ? text.GetRenderedValues(true).x : 0) +
                layoutGroup.padding.right;

            SetWidth(fittedWidth, instant);
        }
    }
}