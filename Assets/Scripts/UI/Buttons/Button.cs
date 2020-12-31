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
        [SerializeField] private ButtonResizer resizer;
        [SerializeField] private ButtonIconSetter iconSetter;

        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color pointerEnterTint = new Color(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color pointerDownTint = new Color(0.9f, 0.9f, 0.9f);

        [Header("Action")]
        [SerializeField] public UnityEvent unityEvent;

        private Action _action;

        private Color _initialBackgroundColour;
        private const float _tintAnimDuration = 0.2f;

        #region Unity

        private void Awake()
        {
            _initialBackgroundColour = backgroundImage.color;
        }

        #endregion

        #region API

        public void Init(ButtonInfo info)
        {
            SetText(info.Message);

            _action = info.Action;

            iconSetter.Set(info.Icon);

            resizer.FitToContent(true);
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

        #endregion

        #region Methods

        private void SetText(string message)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(!String.IsNullOrWhiteSpace(message));
        }

        #endregion
    }
}
