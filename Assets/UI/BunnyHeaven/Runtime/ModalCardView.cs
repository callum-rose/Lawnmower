using System;
using TMPro;
using UnityEngine;

namespace BunnyHeaven.UI
{
    /// <summary>
    /// Full-screen scrim with a white card that pops in (Pause, Level Complete, Level Failed, confirmations).
    /// Put buttons and other content under <see cref="Content"/>.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ModalCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text message;
        [SerializeField] private RectTransform content;
        [SerializeField] private float showTime = 0.25f;
        [SerializeField] private float hideTime = 0.12f;

        private CanvasGroup _group;
        private Coroutine _anim;

        public event Action Shown;
        public event Action Hidden;

        public RectTransform Content => content;
        public TMP_Text Title => title;
        public TMP_Text Message => message;
        public bool IsOpen { get; private set; }

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        public void SetTitle(string text)
        {
            title.text = text;
            title.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void SetMessage(string text)
        {
            message.text = text;
            message.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void Show()
        {
            IsOpen = true;
            gameObject.SetActive(true);
            Group.blocksRaycasts = true;
            Group.interactable = true;
            Animate(showTime, t =>
            {
                Group.alpha = t;
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, UiTween.EaseOutBack(t));
            }, () => Shown?.Invoke());
        }

        public void Hide()
        {
            IsOpen = false;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            Animate(hideTime, t =>
            {
                Group.alpha = 1f - t;
                card.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, t);
            }, () =>
            {
                gameObject.SetActive(false);
                Hidden?.Invoke();
            });
        }

        private void Animate(float duration, Action<float> step, Action done)
        {
            if (_anim != null)
                StopCoroutine(_anim);
            _anim = UiTween.Run(this, duration, step, done);
        }
    }
}
