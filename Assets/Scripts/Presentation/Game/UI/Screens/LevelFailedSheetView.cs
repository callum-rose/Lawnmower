using System;
using BunnyHeaven.UI;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>
	/// The Level Failed sheet (Figma 12:1106): a bottom sheet with the "SCALPED!" tag, the hint and UNDO LAST MOVE /
	/// Restart / Levels, plus a "too short!" annotation over the failed tile. A full-screen blocker covers the board so
	/// these are the only things the player can do. Passive.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	internal class LevelFailedSheetView : MonoBehaviour
	{
		[SerializeField] private RectTransform sheet;
		[SerializeField] private ButtonView undoButton;
		[SerializeField] private ButtonView restartButton;
		[SerializeField] private ButtonView levelsButton;
		[SerializeField] private RectTransform annotation;
		[SerializeField] private float slideTime = 0.3f;

		private CanvasGroup _group;
		private Coroutine _anim;
		private Func<Vector3> _anchor;
		private Camera _camera;

		public ButtonView UndoButton => undoButton;
		public ButtonView RestartButton => restartButton;
		public ButtonView LevelsButton => levelsButton;

		public RectTransform Sheet => sheet;

		public bool IsOpen { get; private set; }

		private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

		/// <summary>The annotation points at this world position (the mower on the failed tile).</summary>
		public void SetAnchor(Func<Vector3> worldPosition, Camera camera)
		{
			_anchor = worldPosition;
			_camera = camera;
		}

		public void Show()
		{
			IsOpen = true;
			gameObject.SetActive(true);
			Group.blocksRaycasts = true;
			Group.interactable = true;
			float height = sheet.rect.height;
			Animate(t =>
			{
				Group.alpha = Mathf.Clamp01(t * 3);
				sheet.anchoredPosition = new Vector2(0, -height * (1 - UiTween.EaseOutQuad(t)));
			}, null);
		}

		public void Hide()
		{
			if (!IsOpen)
			{
				return;
			}

			IsOpen = false;
			Group.blocksRaycasts = false;
			Group.interactable = false;
			float height = sheet.rect.height;
			Animate(t =>
			{
				Group.alpha = 1 - t;
				sheet.anchoredPosition = new Vector2(0, -height * t);
			}, () => gameObject.SetActive(false));
		}

		public void HideImmediate()
		{
			IsOpen = false;
			gameObject.SetActive(false);
		}

		private void LateUpdate()
		{
			if (annotation == null)
			{
				return;
			}

			bool show = IsOpen && _anchor != null && _camera != null;
			if (annotation.gameObject.activeSelf != show)
			{
				annotation.gameObject.SetActive(show);
			}

			if (!show)
			{
				return;
			}

			Vector3 screen = _camera.WorldToScreenPoint(_anchor());
			RectTransform parent = (RectTransform)annotation.parent;
			if (screen.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local))
			{
				annotation.anchoredPosition = local;
			}
		}

		private void Animate(Action<float> step, Action done)
		{
			if (_anim != null)
			{
				StopCoroutine(_anim);
			}

			_anim = UiTween.Run(this, slideTime, step, done);
		}
	}
}
