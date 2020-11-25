using DG.Tweening;
using System.Collections;
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

        #region API

        public void SetWidth(float width, bool instant = false)
        {
            if (instant || !Application.isPlaying)
            {
                RectTransform.sizeDelta = RectTransform.sizeDelta.SetX(width);
                return;
            }

            if (_currentTween?.IsComplete() ?? false)
            {
                _currentTween.Kill();
            }

            Vector2 currentSize = RectTransform.sizeDelta;
            _currentTween = RectTransform.DOSizeDelta(currentSize.SetX(width), animationDuration).SetEase(animationEase);
        }

        public void FitToContent(bool instant = false)
        {
            FitToContent_Internal(icon.gameObject.activeSelf, text.gameObject.activeSelf, instant);
        }

        public void SetContent(bool useIcon, bool useText, bool instant)
        {
            if (instant)
            {
                FitToContent_Internal(useIcon, useText, instant);
            }
            else
            {
                StartCoroutine(FitToContentRoutine(useIcon, useText));
            }
        }

        [Button]
        public void Set(bool useIcon, bool useText, bool instant)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                instant = true;
            }
#endif

            SetContent(useIcon, useText, instant);
        }

        #endregion

        #region Methods

        private void FitToContent_Internal(bool useIcon, bool useText, bool instant)
        {
            float fittedWidth = layoutGroup.padding.left +
                (useIcon ? icon.rectTransform.sizeDelta.x : 0) +
                (useIcon && useText ? layoutGroup.spacing : 0) +
                (useText ? text.preferredWidth : 0) +
                layoutGroup.padding.right;

            SetWidth(fittedWidth, instant);
        }

        #endregion

        #region Routines

        private IEnumerator FitToContentRoutine(bool useIcon, bool useText)
        {
            FitToContent_Internal(useIcon, useText, false);

            yield return _currentTween?.WaitForCompletion();
        }

        #endregion
    }
}