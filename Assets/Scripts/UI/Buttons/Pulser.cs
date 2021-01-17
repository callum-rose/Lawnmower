using Core;
using Core.EventChannels;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Buttons
{
	// [RequireComponent(typeof(Button))]
	[RequireComponent(typeof(Animator))]
	internal class Pulser : MonoBehaviour
	{
		[SerializeField] private UndoableEventChannel doPulseEventChannel;

		// private Button _button;
		private Animator _animator;

		private readonly int _doPulseId = Animator.StringToHash("doPulse");

		private bool IsPulsing => _animator != null && _animator.GetBool(_doPulseId);

		#region Unity

		private void Awake()
		{
			// _button = GetComponent<Button>();
			// _button.unityEvent.AddListener(StopPulse);

			_animator = GetComponent<Animator>();
		}

		private void OnEnable()
		{
			if (doPulseEventChannel != null)
			{
				doPulseEventChannel.EventRaised += OnDoPulseEvent;
			}
		}

		private void OnDisable()
		{
			if (doPulseEventChannel != null)
			{
				doPulseEventChannel.EventRaised -= OnDoPulseEvent;
			}
		}

		#endregion

		#region API

		[Button, DisableInEditorMode, HideIf(nameof(IsPulsing))]
		public void Pulse()
		{
			_animator.SetBool(_doPulseId, true);
		}

		[Button, DisableInEditorMode, ShowIf(nameof(IsPulsing))]
		public void StopPulse()
		{
			_animator.SetBool(_doPulseId, false);
		}

		#endregion

		#region Events

		private void OnDoPulseEvent(Xor isUndo)
		{
			if (!isUndo)
			{
				Pulse();
			}
			else
			{
				StopPulse();
			}
		}

		#endregion
	}
}