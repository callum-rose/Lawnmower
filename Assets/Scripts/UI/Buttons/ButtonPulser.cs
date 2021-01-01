using Core;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Buttons
{
	[RequireComponent(typeof(Button), typeof(Animator))]
	internal class ButtonPulser : MonoBehaviour
	{
		[SerializeField] private VoidEventChannel eventChannel;

		private Button _button;
		private Animator _animator;

		private readonly int _doPulseId = Animator.StringToHash("doPulse");

		private bool IsPulsing => _animator != null && _animator.GetBool(_doPulseId);
		
		#region Unity

		private void Awake()
		{
			_button = GetComponent<Button>();
			_button.unityEvent.AddListener(StopPulse);

			_animator = GetComponent<Animator>();
		}

		private void OnEnable()
		{
			if (eventChannel == null)
			{
				return;
			}

			eventChannel.EventRaised += OnEvent;
		}

		private void OnDisable()
		{
			if (eventChannel == null)
			{
				return;
			}

			eventChannel.EventRaised -= OnEvent;
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

		private void OnEvent()
		{
			Pulse();
		}

		#endregion
	}
}