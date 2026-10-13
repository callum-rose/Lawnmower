using System.Collections;
using TMPro;
using UnityEngine;

namespace BunnyHeaven.UI
{
    /// <summary>Short-lived pill message (e.g. "All levels unlocked!"). Fades and floats in, then out.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text message;
        [SerializeField] private float fadeTime = 0.18f;
        [SerializeField] private float rise = 24f;

        private CanvasGroup _group;
        private Coroutine _routine;
        private Vector2 _restPosition;

        public bool IsShowing { get; private set; }
        public TMP_Text Message => message;

        private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

        private void Awake()
        {
            _restPosition = body.anchoredPosition;
            Group.blocksRaycasts = false;
        }

        public void Show(string text, float seconds = 2f)
        {
            message.text = text;
            gameObject.SetActive(true);
            IsShowing = true;
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(Routine(seconds));
        }

        public void HideImmediate()
        {
            if (_routine != null)
                StopCoroutine(_routine);
            IsShowing = false;
            Group.alpha = 0f;
            gameObject.SetActive(false);
        }

        private IEnumerator Routine(float seconds)
        {
            yield return Fade(0f, 1f);
            yield return new WaitForSecondsRealtime(seconds);
            yield return Fade(1f, 0f);
            IsShowing = false;
            gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to)
        {
            for (var t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                Set(Mathf.Lerp(from, to, UiTween.EaseOutQuad(t / fadeTime)));
                yield return null;
            }

            Set(to);
        }

        private void Set(float visible)
        {
            Group.alpha = visible;
            body.anchoredPosition = _restPosition + Vector2.down * (rise * (1f - visible));
        }
    }
}
