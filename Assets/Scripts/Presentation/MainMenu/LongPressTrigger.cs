using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MainMenu
{
	/// <summary>Raises <see cref="LongPressed"/> once a pointer has been held on this object (or a child) for long enough.</summary>
	public class LongPressTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
	{
		[SerializeField, Min(0.1f)] private float holdSeconds = 2f;

		private float? _heldSince;

		public event Action LongPressed;

		public float HoldSeconds => holdSeconds;

		public bool IsHeld => _heldSince.HasValue;

		public void OnPointerDown(PointerEventData eventData) => _heldSince = Time.unscaledTime;

		public void OnPointerUp(PointerEventData eventData) => _heldSince = null;

		public void OnPointerExit(PointerEventData eventData) => _heldSince = null;

		private void OnDisable() => _heldSince = null;

		private void Update()
		{
			if (_heldSince.HasValue && Time.unscaledTime - _heldSince.Value >= holdSeconds)
			{
				_heldSince = null;
				LongPressed?.Invoke();
			}
		}
	}
}
