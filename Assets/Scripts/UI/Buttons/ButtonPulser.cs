using System;
using System.Collections;
using Core;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI.Buttons
{
	[RequireComponent(typeof(Button), typeof(Animator))]
	internal class ButtonPulser : MonoBehaviour
	{
		[SerializeField] private VoidEventChannel eventChannel;
		[SerializeField] private float pulseScale = 1.2f;
		[SerializeField] private float pulseDuration = 2;

		private Button _button;
		private Animator _animator;

		private Coroutine _pulseRoutine;
		private bool _doStopPulsing;

		private bool PulsePlaying => _pulseRoutine != null && !_doStopPulsing;

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

		[Button, DisableInEditorMode, ShowIf("@!" + nameof(PulsePlaying))]
		public void Pulse()
		{
			if (_pulseRoutine != null)
			{
				return;
			}

			_pulseRoutine = StartCoroutine(PulseRoutine());
		}

		[Button, DisableInEditorMode, ShowIf("@" + nameof(PulsePlaying))]
		public void StopPulse()
		{
			_doStopPulsing = true;
		}

		#endregion

		#region Events

		private void OnEvent()
		{
			Pulse();
		}

		#endregion

		#region Routines

		private IEnumerator PulseRoutine()
		{
			float initialTime = Time.time;

			float GetTimeSinceStart()
			{
				return Time.time - initialTime;
			}

			void SetScale()
			{
				float timeSinceStart = GetTimeSinceStart();
				float t = timeSinceStart * (2f * Mathf.PI) / pulseDuration;
				float cos = Mathf.Cos(t);
				float cos0To1 = 0.5f * (1 - cos);
				float cosLowerToUpper = 1 + cos0To1 * (pulseScale - 1);

				transform.localScale = Vector3.one * cosLowerToUpper;
			}

			while (!_doStopPulsing)
			{
				SetScale();

				yield return null;
			}
			
			_doStopPulsing = false;
			_pulseRoutine = null;

			float timeToStop = Mathf.Ceil(GetTimeSinceStart() / pulseDuration) * pulseDuration;

			while (GetTimeSinceStart() < timeToStop)
			{
				SetScale();

				yield return null;
			}

			transform.localScale = Vector3.one;
		}

		#endregion
	}
}