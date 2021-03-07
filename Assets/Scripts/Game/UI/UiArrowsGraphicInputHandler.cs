using BalsamicBits.Extensions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
	internal class UiArrowsGraphicInputHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler,
		IEndDragHandler
	{
		[SerializeField] private Camera camera;
		[SerializeField] private UiArrowsManager raycaster;
		[SerializeField] private bool isDraggable;
		[SerializeField, Min(0)] private float padding;
		[SerializeField, Min(0)] private float dragTransformScaleFactor = 1.2f;

		private Canvas _parentCanvas;
		private CanvasScaler _rootCanvasScaler;

		private Vector3 _initialScale;
		private Tween _dragScaleUpTween;
		private Vector2 _preDragPosition;

		private void Awake()
		{
			_parentCanvas = GetComponentInParent<Canvas>();
			_rootCanvasScaler = _parentCanvas.rootCanvas.GetComponent<CanvasScaler>();

			_initialScale = transform.localScale;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform,
				eventData.pressPosition, camera, out Vector2 localPoint))
			{
				return;
			}

			Rect rect = RectTransformUtility.PixelAdjustRect(transform as RectTransform, _parentCanvas);
			raycaster.Input(new Vector2((localPoint.x) / rect.width + 0.5f, localPoint.y / rect.height + 0.5f));
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!isDraggable)
			{
				return;
			}

			_preDragPosition = ((RectTransform) transform).anchoredPosition;

			_dragScaleUpTween?.Kill();
			_dragScaleUpTween = transform
				.DOScale(_initialScale * dragTransformScaleFactor, 0.25f)
				.OnComplete(() => _dragScaleUpTween = null);
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!isDraggable)
			{
				return;
			}

			RectTransform rectTransform = ((RectTransform) transform);

			Vector2 delta = eventData.position - eventData.pressPosition;
			rectTransform.anchoredPosition = (_preDragPosition + delta).SetY(rectTransform.anchoredPosition.y);

			float minX = padding + rectTransform.sizeDelta.x * 0.5f;
			if (rectTransform.anchoredPosition.x < minX)
			{
				rectTransform.anchoredPosition = rectTransform.anchoredPosition.SetX(minX);
			}
			else
			{
				float maxX = _rootCanvasScaler.referenceResolution.x - rectTransform.sizeDelta.x * 0.5f - padding;
				if (rectTransform.anchoredPosition.x > maxX)
				{
					rectTransform.anchoredPosition = rectTransform.anchoredPosition.SetX(maxX);
				}
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!isDraggable)
			{
				return;
			}

			_dragScaleUpTween?.Kill();
			_dragScaleUpTween = transform
				.DOScale(_initialScale, 0.25f)
				.OnComplete(() => _dragScaleUpTween = null);
		}
	}
}