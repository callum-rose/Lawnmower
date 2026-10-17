using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Mowers.Input;
using Game.UndoSystem;
using Lawnmower.Model;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Mowers.Models
{
	internal enum FaceExpression
	{
		Neutral,
		Happy,
		Surprised,
		Sad,
		Angry,
		Sleepy,
	}

	/// <summary>
	/// Drives the "Toon Procedural Bunny Face" shader. Instead of swapping decal textures it picks
	/// target expressions and lets springs ease the shader parameters towards them.
	/// </summary>
	internal class ProceduralFaceAnimationComponent : MonoBehaviour, INeedMowerPosition
	{
		[SerializeField] private Material material;

		[Header("Look Around (xy gaze, z converge)")]
		[SerializeField] private Vector3[] lookAroundPoses =
		{
			new Vector3(0.02f, -0.88f, 0.48f),
			new Vector3(-0.75f, 0.05f, 0.22f),
			new Vector3(0.66f, 0.1f, 0.34f),
		};

		[Tooltip("How long the bunny looks dazed after bouncing out of a blocked rabbit hole")]
		[SerializeField, Min(0)] private float dazedDuration = 1.2f;

		[Header("Expressions")]
		[SerializeField] private FacePose neutral = new FacePose
		{
			look = new Vector2(-0.04f, 0.69f), converge = 0.69f, pupilScale = 1f, eyeScale = 1f,
			mouthWidth = 1f,
		};
		[SerializeField] private FacePose happy = new FacePose
		{
			look = new Vector2(0f, 0.5f), converge = 0.5f, pupilScale = 1f, eyeScale = 1f,
			happyEyes = 1f, mouthOpen = 0.45f, mouthWidth = 1f, mouthCurve = 1f,
		};
		[SerializeField] private FacePose surprised = new FacePose
		{
			look = new Vector2(0f, 0.05f), converge = 0.2f, pupilScale = 0.55f, eyeScale = 1.12f,
			mouthOpen = 0.7f, mouthWidth = 0.5f,
		};
		[SerializeField] private FacePose sad = new FacePose
		{
			look = new Vector2(0f, 0.85f), converge = 0.45f, pupilScale = 1.05f, eyeScale = 1f,
			lids = 0.32f, lidTilt = -0.55f, mouthWidth = 0.85f, mouthCurve = -1f,
		};
		[SerializeField] private FacePose angry = new FacePose
		{
			look = new Vector2(0f, 0.35f), converge = 0.75f, pupilScale = 0.85f, eyeScale = 0.95f,
			lids = 0.38f, lidTilt = 0.7f, mouthWidth = 0.9f, mouthCurve = -0.45f,
		};
		[SerializeField] private FacePose sleepy = new FacePose
		{
			look = new Vector2(0f, 0.9f), converge = 0.35f, pupilScale = 1f, eyeScale = 1f,
			lids = 0.6f, lidTilt = -0.1f, mouthWidth = 0.9f,
		};

		[Header("Parameters")]
		[SerializeField] private float durationBeforeLookAround = 3f;
		[SerializeField, Range(0, 1)] private float blinkToLookAroundRatio = 0.33f;
		[SerializeField] private float blinkDuration = 0.2f;
		[SerializeField] private Vector2 lookElsewhereAfterDurationRange = new Vector2(1, 3);
		[SerializeField] private float mouthOpenDuration = 0.25f;
		[SerializeField] private Vector2 noseTwitchIntervalRange = new Vector2(3, 7);
		[SerializeField] private float noseTwitchDuration = 0.6f;
		[SerializeField, Tooltip("Seconds without moving before getting sleepy. 0 disables.")]
		private float sleepyAfterIdleDuration = 15f;

		[Header("Springs")]
		[SerializeField] private SpringSettings lookSpring = new SpringSettings { frequency = 3f, damping = 0.6f };
		[SerializeField] private SpringSettings blinkSpring = new SpringSettings { frequency = 9f, damping = 1f };
		[SerializeField] private SpringSettings mouthSpring = new SpringSettings { frequency = 5f, damping = 0.45f };
		[SerializeField] private SpringSettings expressionSpring = new SpringSettings { frequency = 4f, damping = 0.6f };

		private static readonly int faceLook = Shader.PropertyToID("_FaceLook");
		private static readonly int faceEyes = Shader.PropertyToID("_FaceEyes");
		private static readonly int faceMouth = Shader.PropertyToID("_FaceMouth");
		private static readonly int faceLids = Shader.PropertyToID("_FaceLids");

		private Coroutine _lookRoutine;
		private Coroutine _expressionRoutine;
		private IMowerPosition _mowerPosition;

		private FaceExpression _expression = FaceExpression.Neutral;
		private Vector3 _lookAroundTarget;
		private bool _blinking;
		private bool _hopMouthOpen;
		private float _lastMoveTime;

		private Spring _lookX, _lookY, _converge, _eyes, _mouth;
		private Spring _pupilScale, _eyeScale, _lids, _lidTilt, _happyEyes, _mouthWidth, _mouthCurve;
		private float _eyeShading;
		private float _twitchStartTime = float.NegativeInfinity;
		private float _nextTwitchTime;

		#region Unity

		private void Awake()
		{
			// Keep whatever eye shading the material was authored with
			_eyeShading = material.GetVector(faceEyes).w;

			_lookAroundTarget = new Vector3(neutral.look.x, neutral.look.y, neutral.converge);
			_lookX = new Spring(neutral.look.x);
			_lookY = new Spring(neutral.look.y);
			_converge = new Spring(neutral.converge);
			_eyes = new Spring(1f);
			_mouth = new Spring(neutral.mouthOpen);
			_pupilScale = new Spring(neutral.pupilScale);
			_eyeScale = new Spring(neutral.eyeScale);
			_lids = new Spring(neutral.lids);
			_lidTilt = new Spring(neutral.lidTilt);
			_happyEyes = new Spring(neutral.happyEyes);
			_mouthWidth = new Spring(neutral.mouthWidth);
			_mouthCurve = new Spring(neutral.mouthCurve);
		}

		private void OnEnable()
		{
			_expression = FaceExpression.Neutral;
			_blinking = false;
			_hopMouthOpen = false;
			_lastMoveTime = Time.time;
			_nextTwitchTime = Time.time + RandomInRange(noseTwitchIntervalRange);
			_lookRoutine = StartCoroutine(LookAroundRoutine());
		}

		private void Update()
		{
			if (_expression == FaceExpression.Neutral
			    && sleepyAfterIdleDuration > 0f
			    && Time.time - _lastMoveTime > sleepyAfterIdleDuration)
			{
				_expression = FaceExpression.Sleepy;
			}

			FacePose pose = GetPose(_expression);

			// Only the neutral face wanders; other expressions hold their own gaze
			Vector3 look = _expression == FaceExpression.Neutral
				? _lookAroundTarget
				: new Vector3(pose.look.x, pose.look.y, pose.converge);

			float dt = Time.deltaTime;

			float lookX = _lookX.Step(look.x, lookSpring, dt);
			float lookY = _lookY.Step(look.y, lookSpring, dt);
			float converge = _converge.Step(look.z, lookSpring, dt);
			float eyes = Mathf.Max(0f, _eyes.Step(_blinking ? 0f : 1f, blinkSpring, dt));
			float mouth = _mouth.Step(_hopMouthOpen ? 1f : pose.mouthOpen, mouthSpring, dt);

			float pupilScale = _pupilScale.Step(pose.pupilScale, expressionSpring, dt);
			float eyeScale = _eyeScale.Step(pose.eyeScale, expressionSpring, dt);
			float lids = _lids.Step(pose.lids, expressionSpring, dt);
			float lidTilt = _lidTilt.Step(pose.lidTilt, expressionSpring, dt);
			float happyEyes = _happyEyes.Step(pose.happyEyes, expressionSpring, dt);
			float mouthWidth = _mouthWidth.Step(pose.mouthWidth, expressionSpring, dt);
			float mouthCurve = _mouthCurve.Step(pose.mouthCurve, expressionSpring, dt);

			material.SetVector(faceLook, new Vector4(lookX, lookY, converge, 0f));
			material.SetVector(faceEyes, new Vector4(eyes, eyes, pupilScale, _eyeShading));
			material.SetVector(faceMouth, new Vector4(mouth, mouthWidth, GetNoseSquash(), mouthCurve));
			material.SetVector(faceLids, new Vector4(lids, lidTilt, happyEyes, eyeScale));
		}

		#endregion

		#region API

		/// <summary>
		/// Eases into an expression. With a positive duration it eases back to neutral afterwards,
		/// otherwise it holds until another expression is set.
		/// </summary>
		public void ShowExpression(FaceExpression expression, float duration = 0f)
		{
			if (_expressionRoutine != null)
			{
				StopCoroutine(_expressionRoutine);
				_expressionRoutine = null;
			}

			_expression = expression;
			_lastMoveTime = Time.time;

			if (duration > 0f && expression != FaceExpression.Neutral && isActiveAndEnabled)
			{
				_expressionRoutine = StartCoroutine(ReturnToNeutralRoutine(duration));
			}
		}

		void INeedMowerPosition.Set(IMowerPosition mowerPosition)
		{
			_mowerPosition = mowerPosition;
			_mowerPosition.CurrentPosition.ValueChangedFromTo += OnMowerMoved;
			_mowerPosition.Bounced += OnMowerBounced;
		}

		void INeedMowerPosition.Clear()
		{
			_mowerPosition.CurrentPosition.ValueChangedFromTo -= OnMowerMoved;
			_mowerPosition.Bounced -= OnMowerBounced;
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

		private void OnMowerBounced(IReadOnlyList<PathSegment> attempted)
		{
			if (!isActiveAndEnabled)
			{
				return;
			}

			ShowExpression(FaceExpression.Surprised, dazedDuration);
		}

		#endregion

		#region Methods

		private void Animate()
		{
			_lastMoveTime = Time.time;

			if (_expression == FaceExpression.Sleepy)
			{
				ShowExpression(FaceExpression.Neutral);
			}

			if (_lookRoutine != null)
			{
				StopCoroutine(_lookRoutine);
			}

			_lookRoutine = StartCoroutine(MouthOpenRoutine());
		}

		private IEnumerator MouthOpenRoutine()
		{
			_lookAroundTarget = new Vector3(neutral.look.x, neutral.look.y, neutral.converge);
			_blinking = false;
			_hopMouthOpen = true;

			yield return new WaitForSeconds(mouthOpenDuration);

			_hopMouthOpen = false;

			yield return LookAroundRoutine();
		}

		private IEnumerator LookAroundRoutine()
		{
			_lookAroundTarget = new Vector3(neutral.look.x, neutral.look.y, neutral.converge);

			yield return new WaitForSeconds(durationBeforeLookAround);

			while (true)
			{
				if (Random.value < blinkToLookAroundRatio)
				{
					_blinking = true;
					yield return new WaitForSeconds(blinkDuration * 0.5f);

					_blinking = false;
					yield return new WaitForSeconds(blinkDuration * 0.5f);
				}

				_lookAroundTarget = lookAroundPoses[Random.Range(0, lookAroundPoses.Length)];

				yield return new WaitForSeconds(RandomInRange(lookElsewhereAfterDurationRange));
			}
		}

		private IEnumerator ReturnToNeutralRoutine(float duration)
		{
			yield return new WaitForSeconds(duration);

			_expressionRoutine = null;
			_expression = FaceExpression.Neutral;
			_lastMoveTime = Time.time;
		}

		private FacePose GetPose(FaceExpression expression)
		{
			switch (expression)
			{
				case FaceExpression.Happy: return happy;
				case FaceExpression.Surprised: return surprised;
				case FaceExpression.Sad: return sad;
				case FaceExpression.Angry: return angry;
				case FaceExpression.Sleepy: return sleepy;
				default: return neutral;
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
		private struct FacePose
		{
			[Tooltip("Gaze direction, -1..1. +y looks down")] public Vector2 look;
			[Tooltip("How cross-eyed the pupils are")] public float converge;
			public float pupilScale;
			public float eyeScale;
			[Range(0, 1), Tooltip("How far the lids are lowered")] public float lids;
			[Range(-1, 1), Tooltip("+ lowers inner corners (angry), - lowers outer corners (sad)")] public float lidTilt;
			[Range(0, 1), Tooltip("Closed ^ ^ eyes")] public float happyEyes;
			[Range(0, 1)] public float mouthOpen;
			public float mouthWidth;
			[Range(-1, 1), Tooltip("+ smile, - frown")] public float mouthCurve;
		}

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
