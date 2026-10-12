using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>Squashes the target while a pointer is held down on it, and springs back on release.</summary>
    public class PressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float speed = 22f;

        private Selectable _selectable;
        private float _goal = 1f;
        private float _current = 1f;

        public bool IsPressed { get; private set; }

        private RectTransform Target => target != null ? target : (RectTransform)transform;

        private void Awake() => _selectable = GetComponent<Selectable>();

        private void OnDisable()
        {
            IsPressed = false;
            _goal = _current = 1f;
            Target.localScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.IsInteractable())
                return;

            IsPressed = true;
            _goal = pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            IsPressed = false;
            _goal = 1f;
        }

        private void Update()
        {
            if (Mathf.Approximately(_current, _goal))
                return;

            _current = Mathf.Lerp(_current, _goal, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (Mathf.Abs(_current - _goal) < 0.002f)
                _current = _goal;

            Target.localScale = new Vector3(_current, _current, 1f);
        }
    }
}
