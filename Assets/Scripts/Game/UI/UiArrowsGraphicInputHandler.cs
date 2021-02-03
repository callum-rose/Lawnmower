using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Game.UI
{
	internal class UiArrowsGraphicInputHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		[SerializeField] private Camera camera;
		[SerializeField] private UiArrowsCameraRaycaster raycaster;
		
		private Canvas _parentCanvas;

		private void Awake()
		{
			_parentCanvas = GetComponentInParent<Canvas>();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform,
				eventData.pressPosition, camera, out Vector2 localPoint))
			{
				return;
			}

			Rect rect = RectTransformUtility.PixelAdjustRect(transform as RectTransform, _parentCanvas);
			raycaster.Raycast(new Vector2(localPoint.x / rect.width, localPoint.y / rect.height));
		}

		public void OnPointerUp(PointerEventData eventData)
		{
		}
	}
}