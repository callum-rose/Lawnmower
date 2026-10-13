using System;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using UnityEngine;

namespace Lawnmower.Screens
{
	/// <summary>
	/// Root of the Settings overlay prefab: owns the <see cref="SettingsPresenter"/> for its lifetime and navigates
	/// (close, replay tutorial). Open it through <see cref="SettingsOverlay.Show"/>.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public class SettingsOverlayView : MonoBehaviour, ISettingsNavigator
	{
		public const string TutorialNotWiredMessage = "Tutorial coming soon!";

		[SerializeField] private SettingsView view;
		[SerializeField] private float fadeTime = 0.15f;

		private SettingsPresenter _presenter;
		private CanvasGroup _group;
		private Action _onClosed;
		private bool _isClosing;

		public SettingsView View => view;
		public SettingsPresenter Presenter => _presenter;

		private CanvasGroup Group => _group != null ? _group : _group = GetComponent<CanvasGroup>();

		internal void Open(GameSettings settings, Progression progression, string version, Action onClosed)
		{
			_onClosed = onClosed;
			_presenter = new SettingsPresenter(view, settings, progression, this, version);
			_presenter.Start();

			Group.alpha = 0f;
			Group.blocksRaycasts = true;
			UiTween.Run(this, fadeTime, t => Group.alpha = t);
		}

		internal void AddClosedCallback(Action onClosed) => _onClosed += onClosed;

		public void Close()
		{
			if (_isClosing)
			{
				return;
			}

			_isClosing = true;
			_presenter?.Dispose();
			_presenter = null;
			Group.blocksRaycasts = false;
			UiTween.Run(this, fadeTime, t => Group.alpha = 1f - t, Finish);
		}

		public void ReplayTutorial()
		{
			Action hook = SettingsOverlay.ReplayTutorialHook;

			if (hook == null)
			{
				Debug.LogWarning("[SettingsOverlay] Replay tutorial: SettingsOverlay.ReplayTutorialHook isn't set yet.");
				view.ShowMessage(TutorialNotWiredMessage);
				return;
			}

			_onClosed += hook;
			Close();
		}

		private void OnDestroy()
		{
			// Destroyed by a scene change while open: still tidy up.
			if (!_isClosing)
			{
				_presenter?.Dispose();
				_presenter = null;
				SettingsOverlay.NotifyClosed(this);
			}
		}

		private void Finish()
		{
			Action onClosed = _onClosed;
			_onClosed = null;
			SettingsOverlay.NotifyClosed(this);
			Destroy(gameObject);
			onClosed?.Invoke();
		}
	}
}
