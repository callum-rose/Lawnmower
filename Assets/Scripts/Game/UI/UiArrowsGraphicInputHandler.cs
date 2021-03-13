using System;
using BalsamicBits.Extensions;
using DG.Tweening;
using Game.Core;
using Game.Mowers.Input;
using Sirenix.OdinInspector;
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
		[SerializeField] private RenderTexture renderTexture;

		[TitleGroup("Colour")]
		[SerializeField] private RawImage image;

		[SerializeField] private Color beingUsedColour;
		[SerializeField] private Color notBeingUsedColour;
		[SerializeField] private float colourChangeDuration;

		[TitleGroup("Dragging")]
		[SerializeField] private bool isDraggable;

		[SerializeField, Min(0)] private float padding;
		[SerializeField, Min(0)] private float dragTransformScaleFactor = 1.2f;

		[TitleGroup("Event Channels")]
		[SerializeField] private IMowerInputEventChannelListenerContainer mowerInputEventChannelContainer;

		private IMowerInputEventChannelListener MowerInputEventChannelListener =>
			mowerInputEventChannelContainer.Result;

		private RectTransform _rectTransform;
		private Canvas _parentCanvas;
		private CanvasScaler _rootCanvasScaler;

		private Vector3 _initialScale;
		private Tween _dragScaleUpTween;
		private Vector2 _preDragPosition;

		private int _lastInputFrame;
		private bool _isBeingUsed;

		private void Awake()
		{
			_rectTransform = transform as RectTransform;

			_parentCanvas = GetComponentInParent<Canvas>();
			_rootCanvasScaler = _parentCanvas.rootCanvas.GetComponent<CanvasScaler>();

			_initialScale = transform.localScale;

			MowerInputEventChannelListener.EventRaised += MowerInputEventChannelOnEventRaised;
		}

		private void Start()
		{
			UpdateRenderTextureSize();
		}

		private void UpdateRenderTextureSize()
		{
			Matrix4x4 localToWorldMatrix = _rectTransform.localToWorldMatrix;
			Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, localToWorldMatrix * _rectTransform.rect.min);
			Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, localToWorldMatrix * _rectTransform.rect.max);
			Vector2 diff = topRight - bottomLeft;
			renderTexture.width = (int) diff.x;
			renderTexture.height = (int) diff.y;
		}

		private void OnEnable()
		{
			_isBeingUsed = true;
			SetBeingUsedAppearance(true);
		}

		private void OnDestroy()
		{
			MowerInputEventChannelListener.EventRaised -= MowerInputEventChannelOnEventRaised;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform,
				eventData.pressPosition, camera, out Vector2 localPoint))
			{
				return;
			}

			_lastInputFrame = Time.frameCount;

			Rect rect = RectTransformUtility.PixelAdjustRect(_rectTransform, _parentCanvas);
			Vector2 pivot = _rectTransform.pivot;
			raycaster.Input(new Vector2(localPoint.x / rect.width + pivot.x, localPoint.y / rect.height + pivot.y));
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

		private void MowerInputEventChannelOnEventRaised(GridVector _)
		{
			SetBeingUsedAppearance();
		}

		private void SetBeingUsedAppearance(bool force = false)
		{
			bool lastLocalInputWasThisFrame = Time.frameCount == _lastInputFrame;

			if (!force && lastLocalInputWasThisFrame == _isBeingUsed)
			{
				return;
			}

			image.DOKill();
			image.DOColor(lastLocalInputWasThisFrame ? beingUsedColour : notBeingUsedColour, colourChangeDuration);

			_isBeingUsed = lastLocalInputWasThisFrame;
		}
	}
}