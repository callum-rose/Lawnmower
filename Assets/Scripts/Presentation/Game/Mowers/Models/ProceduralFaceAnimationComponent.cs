using System;
using System.Collections;
using Game.Core;
using Game.Mowers.Input;
using Game.UndoSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Mowers.Models
{
	/// <summary>
	/// Drives the "Toon Procedural Bunny Face" shader. Instead of swapping decal textures it picks
	/// target expressions and lets springs ease the shader parameters towards them.
	/// </summary>
	internal class ProceduralFaceAnimationComponent : MonoBehaviour, INeedMowerPosition
	{
		[SerializeField] private Material material;

		[Header("Poses (xy gaze, z converge)")]
		[SerializeField] private Vector3 idleLook = new Vector3(-0.04f, 0.69f, 0.69f);
		[SerializeField] private Vector3[] lookAroundPoses =
		{
			new Vector3(0.02f, -0.88f, 0.48f),
			new Vector3(-0.75f, 0.05f, 0.22f),
			new Vector3(0.66f, 0.1f, 0.34f),
		};

		[Header("Parameters")]
		[SerializeField] private float durationBeforeLookAround = 3f;
		[SerializeField, Range(0, 1)] private float blinkToLookAroundRatio = 0.33f;
		[SerializeField] private float blinkDuration = 0.2f;
		[SerializeField] private Vector2 lookElsewhereAfterDurationRange = new Vector2(1, 3);
		[SerializeField] private float mouthOpenDuration = 0.25f;
		[SerializeField] private Vector2 noseTwitchIntervalRange = new Vector2(3, 7);
		[SerializeField] private float noseTwitchDuration = 0.6f;

		[Header("Springs")]
		[SerializeField] private SpringSettings lookSpring = new SpringSettings { frequency = 3f, damping = 0.6f };
		[SerializeField] private SpringSettings blinkSpring = new SpringSettings { frequency = 9f, damping = 1f };
		[SerializeField] private SpringSettings mouthSpring = new SpringSettings { frequency = 5f, damping = 0.45f };

		private static readonly int faceLook = Shader.PropertyToID("_FaceLook");
		private static readonly int faceEyes = Shader.PropertyToID("_FaceEyes");
		private static readonly int faceMouth = Shader.PropertyToID("_FaceMouth");

		private Coroutine _currentRoutine;
		private IMowerPosition _mowerPosition;

		private Vector3 _lookTarget;
		private float _eyeTarget = 1f;
		private float _mouthTarget;

		private Spring _lookX, _lookY, _converge, _eyes, _mouth;
		private Vector4 _baseEyes, _baseMouth;
		private float _twitchStartTime = float.NegativeInfinity;
		private float _nextTwitchTime;

		#region Unity

		private void Awake()
		{
			// Keep whatever pupil scale, shading and mouth width the material was authored with
			_baseEyes = material.GetVector(faceEyes);
			_baseMouth = material.GetVector(faceMouth);

			_lookTarget = idleLook;
			_lookX = new Spring(idleLook.x);
			_lookY = new Spring(idleLook.y);
			_converge = new Spring(idleLook.z);
			_eyes = new Spring(1f);
			_mouth = new Spring(0f);
		}

		private void OnEnable()
		{
			_eyeTarget = 1f;
			_mouthTarget = 0f;
			_nextTwitchTime = Time.time + RandomInRange(noseTwitchIntervalRange);
			_currentRoutine = StartCoroutine(LookAroundRoutine());
		}

		private void Update()
		{
			float dt = Time.deltaTime;

			float lookX = _lookX.Step(_lookTarget.x, lookSpring, dt);
			float lookY = _lookY.Step(_lookTarget.y, lookSpring, dt);
			float converge = _converge.Step(_lookTarget.z, lookSpring, dt);
			float eyes = Mathf.Max(0f, _eyes.Step(_eyeTarget, blinkSpring, dt));
			float mouth = _mouth.Step(_mouthTarget, mouthSpring, dt);

			material.SetVector(faceLook, new Vector4(lookX, lookY, converge, 0f));
			material.SetVector(faceEyes, new Vector4(eyes, eyes, _baseEyes.z, _baseEyes.w));
			material.SetVector(faceMouth, new Vector4(mouth, _baseMouth.y, GetNoseSquash(), 0f));
		}

		#endregion

		#region API

		void INeedMowerPosition.Set(IMowerPosition mowerPosition)
		{
			_mowerPosition = mowerPosition;
			_mowerPosition.CurrentPosition.ValueChangedFromTo += OnMowerMoved;
		}

		void INeedMowerPosition.Clear()
		{
			_mowerPosition.CurrentPosition.ValueChangedFromTo -= OnMowerMoved;
			_mowerPosition = null;
		}

		#endregion

		#region Events

		private void OnMowerMoved(GridVector from, GridVector to, Xor inverted)
		{
			if (inverted || !isActiveAndEnabled)
			{
				return;
			}

			Animate();
		}

		#endregion

		#region Methods

		private void Animate()
		{
			if (_currentRoutine != null)
			{
				StopCoroutine(_currentRoutine);
			}

			_currentRoutine = StartCoroutine(MouthOpenRoutine());
		}

		private IEnumerator MouthOpenRoutine()
		{
			_lookTarget = idleLook;
			_eyeTarget = 1f;
			_mouthTarget = 1f;

			yield return new WaitForSeconds(mouthOpenDuration);

			_mouthTarget = 0f;

			yield return LookAroundRoutine();
		}

		private IEnumerator LookAroundRoutine()
		{
			_lookTarget = idleLook;

			yield return new WaitForSeconds(durationBeforeLookAround);

			while (true)
			{
				if (Random.value < blinkToLookAroundRatio)
				{
					_eyeTarget = 0f;
					yield return new WaitForSeconds(blinkDuration * 0.5f);

					_eyeTarget = 1f;
					yield return new WaitForSeconds(blinkDuration * 0.5f);
				}

				_lookTarget = lookAroundPoses[Random.Range(0, lookAroundPoses.Length)];

				yield return new WaitForSeconds(RandomInRange(lookElsewhereAfterDurationRange));
			}
		}

		private float GetNoseSquash()
		{
			if (Time.time >= _nextTwitchTime)
			{
				_twitchStartTime = Time.time;
				_nextTwitchTime = Time.time + RandomInRange(noseTwitchIntervalRange);
			}

			float t = Time.time - _twitchStartTime;

			if (t >= noseTwitchDuration)
			{
				return 0f;
			}

			return Mathf.Sin(t * 45f) * Mathf.Sin(Mathf.PI * t / noseTwitchDuration);
		}

		private static float RandomInRange(Vector2 range)
		{
			return Random.Range(range.x, range.y);
		}

		#endregion

		[Serializable]
		private struct SpringSettings
		{
			[Tooltip("Oscillations per second")] public float frequency;
			[Tooltip("1 = no overshoot, lower = bouncier")] public float damping;
		}

		private struct Spring
		{
			private float _value;
			private float _velocity;

			public Spring(float value)
			{
				_value = value;
				_velocity = 0f;
			}

			public float Step(float target, SpringSettings settings, float dt)
			{
				// Implicit Euler so stiff springs stay stable at any frame time
				float w = 2f * Mathf.PI * settings.frequency;
				_velocity = (_velocity - dt * w * w * (_value - target)) / (1f + 2f * settings.damping * w * dt + w * w * dt * dt);
				_value += _velocity * dt;
				return _value;
			}
		}
	}
}
