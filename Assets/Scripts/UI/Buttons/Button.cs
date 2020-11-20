using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Buttons
{
    [RequireComponent(typeof(Graphic))]
    internal class Button : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image iconImage;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color pointerEnterTint = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color pointerDownTint = new Color(0.9f, 0.9f, 0.9f);

        [Header("Action")]
        [SerializeField] private UnityEvent unityEvent;

        private Action _action;

        private Color _initialBackgroundColour;
        private const float _tintAnimDuration = 0.2f;

        private void Awake()
        {
            _initialBackgroundColour = backgroundImage.color;
        }

        public void Init(ButtonInfo info)
        {
            messageText.text = info.Message;

            iconImage.sprite = info.Icon;
            iconImage.gameObject.SetActive(info.Icon != null);

            _action = info.Action;

            GetComponent<ButtonResizer>().FitToContent(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _action?.Invoke();
            unityEvent.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {

        }

        public void OnPointerUp(PointerEventData eventData)
        {
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                OnPointerExit(eventData);
                return;
            }

            backgroundImage.DOColor(_initialBackgroundColour * pointerEnterTint, _tintAnimDuration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            backgroundImage.DOColor(_initialBackgroundColour, _tintAnimDuration);
        }

        #region Odin

        private void OnShadowOffsetValueChanged(Vector2 newOffset)
        {
        }

        #endregion
    }
}
