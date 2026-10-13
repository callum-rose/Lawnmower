using System;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Screens
{
	/// <summary>
	/// The tutorial hint card under the top bar. It never blocks touches on the board; the last step dismisses
	/// itself after <see cref="dismissAfter"/> seconds. Passive.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	internal class TutorialOverlayView : MonoBehaviour, ITutorialView
	{
		[SerializeField] private RectTransform card;
		[SerializeField] private TMP_Text message;
		[SerializeField] private Image icon;
		[SerializeField] private Sprite moveIcon;
		[SerializeField] private Sprite mowIcon;
		[SerializeField] private float dismissAfter = 4f;
		[SerializeField] private float fadeTime = 0.2f;

		private CanvasGroup _group;
		private Coroutine _anim;
		private float _dismissAt = -1;

		public event Action Dismissed;

		public TutorialStep? Step { get; private set; }
		public string Message => message.text;

		private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

		public void ShowStep(TutorialStep step)
		{
			Step = step;
			message.text = step switch
			{
				TutorialStep.Swipe => "Swipe on the lawn to move the bunny",
				TutorialStep.TapArrows => "Tap the arrows to move the bunny",
				_ => "Mow the rest of the garden!",
			};
			icon.sprite = step == TutorialStep.MowTheRest ? mowIcon : moveIcon;
			_dismissAt = step == TutorialStep.MowTheRest ? Time.unscaledTime + dismissAfter : -1;

			gameObject.SetActive(true);
			Group.blocksRaycasts = false;
			Group.interactable = false;
			Animate(t =>
			{
				Group.alpha = t;
				card.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, UiTween.EaseOutBack(t));
			}, null);
		}

		public void Hide()
		{
			// The scene may be unloading (the presenter is disposed from GameManager.OnDestroy).
			if (this == null)
			{
				return;
			}

			Step = null;
			_dismissAt = -1;

			if (!gameObject.activeInHierarchy)
			{
				gameObject.SetActive(false);
				return;
			}

			Animate(t => Group.alpha = 1 - t, () => gameObject.SetActive(false));
		}

		/// <summary>For tests: as if the last step's timer ran out.</summary>
		internal void Dismiss()
		{
			_dismissAt = -1;
			Dismissed?.Invoke();
		}

		private void Update()
		{
			if (_dismissAt >= 0 && Time.unscaledTime >= _dismissAt)
			{
				Dismiss();
			}
		}

		private void Animate(Action<float> step, Action done)
		{
			if (_anim != null)
			{
				StopCoroutine(_anim);
			}

			_anim = UiTween.Run(this, fadeTime, step, done);
		}
	}
}
