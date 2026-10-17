using System.Collections;
using Game.Mowers.Models;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MainMenu
{
	/// <summary>
	/// The Main Menu's showcase bunny. Cycles through the face expressions with a little hop on each change,
	/// and turns gently from side to side in between. While the screen is pressed it watches the finger instead.
	/// </summary>
	public class HeroBunny : MonoBehaviour
	{
		[SerializeField] private ProceduralFaceAnimationComponent face;
		[SerializeField] private Transform body;
		[SerializeField] private CameraRawImage portrait;

		[SerializeField] private FaceExpression[] expressions =
		{
			FaceExpression.Happy,
			FaceExpression.Neutral,
			FaceExpression.Surprised,
			FaceExpression.Sad,
			FaceExpression.Angry,
			FaceExpression.Sleepy,
		};

		[Header("Timing")]
		[SerializeField] private float holdDuration = 2.2f;

		[Header("Hop")]
		[SerializeField] private float hopHeight = 0.25f;
		[SerializeField] private float hopDuration = 0.35f;

		[Header("Gaze")]
		[SerializeField, Tooltip("Offset from the body to the point between the eyes")]
		private Vector3 faceOffset = new Vector3(0f, 0.15f, 0f);
		[SerializeField, Tooltip("Distance from the face, in image heights, at which the eyes are fully turned")]
		private float gazeReach = 0.6f;

		[Header("Sway")]
		[SerializeField] private float swayAngle = 12f;
		[SerializeField] private float swayPeriod = 6f;

		private Vector3 _restPosition;
		private Quaternion _restRotation;
		private float _hopStartTime = float.NegativeInfinity;
		private bool _isPressed;
		private Material _material;

		#region Unity

		private void Awake()
		{
			_restPosition = body.localPosition;
			_restRotation = body.localRotation;

			// Animate a copy so playing doesn't keep rewriting the material asset
			_material = new Material(face.Material);
			face.Material = _material;

			foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>())
			{
				renderer.sharedMaterial = _material;
			}
		}

		private void OnDestroy()
		{
			Destroy(_material);
		}

		private void OnEnable()
		{
			StartCoroutine(CycleRoutine());
		}

		private void Update()
		{
			Pointer pointer = Pointer.current;
			bool isPressed = pointer != null && pointer.press.isPressed;

			if (isPressed && !_isPressed)
			{
				// Closed or sleepy eyes can't follow anything
				face.ShowExpression(FaceExpression.Neutral);
			}

			_isPressed = isPressed;
			face.SetLookOverride(isPressed ? GetLookTowards(pointer.position.ReadValue()) : null);

			float hopT = (Time.time - _hopStartTime) / hopDuration;
			float hop = hopT is >= 0f and < 1f ? Mathf.Sin(hopT * Mathf.PI) * hopHeight : 0f;

			body.localPosition = _restPosition + Vector3.up * hop;
			body.localRotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 2f * Mathf.PI / swayPeriod) * swayAngle, Vector3.up)
			                     * _restRotation;
		}

		#endregion

		private IEnumerator CycleRoutine()
		{
			var wait = new WaitForSeconds(holdDuration);

			for (int i = 0; ; i = (i + 1) % expressions.Length)
			{
				while (_isPressed)
				{
					yield return null;
				}

				face.ShowExpression(expressions[i]);
				_hopStartTime = Time.time;

				yield return wait;
			}
		}

		private Vector2 GetLookTowards(Vector2 screenPoint)
		{
			Camera camera = portrait.SourceCamera;
			Vector2 faceViewport = camera.WorldToViewportPoint(body.position + faceOffset);
			Vector2 delta = portrait.ScreenToViewportPoint(screenPoint) - faceViewport;
			delta.x *= camera.aspect;

			Vector2 look = Vector2.ClampMagnitude(delta / gazeReach, 1f);

			// The face's gaze has +y looking down
			return new Vector2(look.x, -look.y);
		}
	}
}
