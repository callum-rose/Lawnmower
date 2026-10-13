using Game.Core;
using Game.Input;
using Game.Levels;
using Game.UI.Screens;
using Lawnmower.Model;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Cameras
{
	/// <summary>
	/// Frames the whole board into the screen rectangle the HUD leaves free (<see cref="GameScreenView.GetBoardScreenRect"/>):
	/// a fixed-angle camera is moved (distance and sideways) so the board's bounds just fit, for any level size,
	/// aspect and safe area. Drives a high-priority, procedural-free <see cref="CinemachineCamera"/>, so the brain
	/// renders it over the old follow/level rigs.
	/// <para>
	/// On top of that fit the player can pinch to zoom, twist to rotate and drag with two fingers to pan (the mouse wheel
	/// zooms in the editor). The board point under the fingers stays under them. When the fingers lift, the rotation
	/// eases to the nearest quarter turn, so the board's axes (and so swipe directions) stay readable. The view resets
	/// for each new level.
	/// </para>
	/// </summary>
	[DefaultExecutionOrder(-100)]
	internal class BoardCameraFramer : MonoBehaviour
	{
		[SerializeField] private CinemachineCamera boardCamera;
		[SerializeField] private LevelManager levelManager;
		[SerializeField] private Positioner positioner;
		[SerializeField] private GameScreenView screen;

		[Header("View")]
		[SerializeField] private float pitch = 42.9f;
		[SerializeField] private float yaw = 21.8f;
		[SerializeField] private float fieldOfView = 7;

		[Tooltip("Board bounds: below the tile tops (the soil rim) and above them (grass, the mower).")]
		[SerializeField] private float boundsBelow = 0.6f;
		[SerializeField] private float boundsAbove = 0.9f;
		[SerializeField] private float boundsPadding = 0.15f;

		[SerializeField, Min(0)] private float smoothTime = 0.25f;

		[Header("Gestures")]
		[SerializeField, Min(1)] private float maxZoom = 4;
		[Tooltip("Degrees per second the rotation eases to the nearest quarter turn once the fingers lift.")]
		[SerializeField, Min(0)] private float rotationSnapSpeed = 360;
		[SerializeField, Min(1)] private float mouseWheelZoomStep = 1.15f;

		private Level _framedLevel;
		/// <summary>The fitted camera as (distance back along the view, sideways, lift) from the board centre.</summary>
		private Vector3 _fit;
		private Vector3 _fitVelocity;
		private readonly Vector3[] _corners = new Vector3[8];

		private float _zoom = 1;
		private float _yawOffset;
		/// <summary>World offset (XZ) of the camera from the fitted view.</summary>
		private Vector3 _pan;

		private bool _isGesturing;
		private int _touchIdA, _touchIdB;
		private bool _isGestureOverUi;
		private Vector2 _lastA, _lastB;

		/// <summary>How far the player has zoomed in: 1 is the whole board fitted.</summary>
		public float Zoom => _zoom;

		/// <summary>How far the player has turned the board from the default view, in degrees.</summary>
		public float YawOffset => _yawOffset;

		public Rect LastTarget { get; private set; }

		private void LateUpdate()
		{
			Level level = levelManager.Level;
			if (level == null || boardCamera == null || Screen.width <= 0 || Screen.height <= 0)
			{
				_framedLevel = null;
				_isGesturing = false;
				return;
			}

			Rect target = screen != null ? screen.GetBoardScreenRect() : new Rect(0, 0, Screen.width, Screen.height);
			LastTarget = target;
			GetBoardBounds(level, out Vector3 min, out Vector3 max);

			bool isNewLevel = _framedLevel != level;
			if (isNewLevel)
			{
				_framedLevel = level;
				ResetView();
			}

			if (!isNewLevel)
			{
				UpdateGestures(min, max);
			}

			if (!_isGesturing)
			{
				SnapRotation();

				// The fit depends on the rotation, so it's only re-solved between gestures: mid-gesture the board stays
				// exactly under the fingers.
				Vector3 fit = Solve(min, max, Rotation, target);
				if (isNewLevel || smoothTime <= 0)
				{
					_fit = fit;
					_fitVelocity = Vector3.zero;
					boardCamera.PreviousStateIsValid = false;
				}
				else
				{
					_fit = Vector3.SmoothDamp(_fit, fit, ref _fitVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
				}
			}

			LensSettings lens = boardCamera.Lens;
			lens.FieldOfView = fieldOfView;
			lens.NearClipPlane = 10 / _zoom;
			lens.FarClipPlane = 600;
			boardCamera.Lens = lens;
			boardCamera.transform.SetPositionAndRotation(CameraPosition(min, max, _zoom, _yawOffset, _pan), Rotation);
		}

		/// <summary>Back to the fitted, unrotated view.</summary>
		private void ResetView()
		{
			_zoom = 1;
			_yawOffset = 0;
			_pan = Vector3.zero;
			_isGesturing = false;
		}

		#region Gestures

		private Quaternion Rotation => RotationFor(_yawOffset);

		private Quaternion RotationFor(float yawOffset) => Quaternion.Euler(pitch, yaw + yawOffset, 0);

		private void UpdateGestures(Vector3 min, Vector3 max)
		{
			if (TryGetTwoTouches(out TouchControl a, out TouchControl b))
			{
				int idA = a.touchId.ReadValue(), idB = b.touchId.ReadValue();
				Vector2 positionA = a.position.ReadValue(), positionB = b.position.ReadValue();

				if (!_isGesturing || idA != _touchIdA || idB != _touchIdB)
				{
					// A new pair of fingers: start from here. Pairs that started on the HUD are ignored.
					_isGesturing = true;
					_touchIdA = idA;
					_touchIdB = idB;
					_isGestureOverUi = ScreenTouches.IsOverCanvas(a.startPosition.ReadValue()) ||
					                   ScreenTouches.IsOverCanvas(b.startPosition.ReadValue());
				}
				else if (!_isGestureOverUi)
				{
					ApplyGesture(min, max, _lastA, _lastB, positionA, positionB);
				}

				_lastA = positionA;
				_lastB = positionB;
				return;
			}

			_isGesturing = false;

			Mouse mouse = Mouse.current;
			float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0;
			if (scroll != 0 && ScreenTouches.CountPressed() == 0)
			{
				Vector2 position = mouse.position.ReadValue();
				if (!ScreenTouches.IsOverCanvas(position))
				{
					float scale = scroll > 0 ? mouseWheelZoomStep : 1 / mouseWheelZoomStep;
					Vector2 spread = Vector2.right * 100;
					ApplyGesture(min, max, position, position + spread, position, position + spread * scale);
				}
			}
		}

		private static bool TryGetTwoTouches(out TouchControl a, out TouchControl b)
		{
			a = b = null;
			Touchscreen touchscreen = Touchscreen.current;
			if (touchscreen == null)
			{
				return false;
			}

			foreach (TouchControl touch in touchscreen.touches)
			{
				if (!touch.press.isPressed)
				{
					continue;
				}

				if (a == null)
				{
					a = touch;
				}
				else
				{
					b = touch;
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The fingers moved from (<paramref name="fromA"/>, <paramref name="fromB"/>) to (<paramref name="toA"/>,
		/// <paramref name="toB"/>): zoom by their change in spread, turn by their change in angle, and pan so the board
		/// point that was under their midpoint is under it again.
		/// </summary>
		private void ApplyGesture(Vector3 min, Vector3 max, Vector2 fromA, Vector2 fromB, Vector2 toA, Vector2 toB)
		{
			Vector2 from = fromB - fromA, to = toB - toA;
			if (from.sqrMagnitude < 1 || to.sqrMagnitude < 1)
			{
				return;
			}

			float boardY = max.y - boundsAbove;
			Vector3 anchor = BoardPointUnder((fromA + fromB) * 0.5f, CameraPosition(min, max, _zoom, _yawOffset, _pan),
				Rotation, boardY);

			_zoom = Mathf.Clamp(_zoom * to.magnitude / from.magnitude, 1, maxZoom);
			// Fingers turning anticlockwise on screen turn the board anticlockwise: the camera yaws clockwise.
			_yawOffset = Mathf.Repeat(_yawOffset + Vector2.SignedAngle(from, to) + 180, 360) - 180;

			Vector3 landed = BoardPointUnder((toA + toB) * 0.5f, CameraPosition(min, max, _zoom, _yawOffset, _pan),
				Rotation, boardY);
			_pan += Vector3.ProjectOnPlane(anchor - landed, Vector3.up);
			_pan = ClampPan(_pan, min, max);
		}

		/// <summary>Panning is limited so the centre of the view stays over the board; none at all fully zoomed out.</summary>
		private Vector3 ClampPan(Vector3 pan, Vector3 min, Vector3 max)
		{
			float freedom = 1 - 1 / _zoom;
			float x = (max.x - min.x) * 0.5f * freedom, z = (max.z - min.z) * 0.5f * freedom;
			return new Vector3(Mathf.Clamp(pan.x, -x, x), 0, Mathf.Clamp(pan.z, -z, z));
		}

		private void SnapRotation()
		{
			float nearest = Mathf.Round(_yawOffset / 90) * 90;
			_yawOffset = Mathf.MoveTowards(_yawOffset, nearest, rotationSnapSpeed * Time.unscaledDeltaTime);
			if (Mathf.Approximately(_yawOffset, nearest))
			{
				_yawOffset = Mathf.Repeat(nearest + 180, 360) - 180;
			}
		}

		/// <summary>Where the camera goes for this zoom, turn and pan, given the current fit.</summary>
		private Vector3 CameraPosition(Vector3 min, Vector3 max, float zoom, float yawOffset, Vector3 pan)
		{
			Quaternion rotation = RotationFor(yawOffset);
			Vector3 forward = rotation * Vector3.forward;
			Vector3 fitted = (min + max) * 0.5f - forward * _fit.x + rotation * Vector3.right * _fit.y + rotation * Vector3.up * _fit.z;
			return fitted + forward * (_fit.x * (1 - 1 / zoom)) + pan;
		}

		/// <summary>The point at height <paramref name="boardY"/> seen at <paramref name="screenPoint"/> from this camera.</summary>
		private Vector3 BoardPointUnder(Vector2 screenPoint, Vector3 position, Quaternion rotation, float boardY)
		{
			float tanV = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
			float tanH = tanV * Screen.width / Screen.height;
			Vector3 direction = rotation * new Vector3(
				(screenPoint.x / Screen.width * 2 - 1) * tanH,
				(screenPoint.y / Screen.height * 2 - 1) * tanV,
				1);

			if (direction.y > -0.0001f)
			{
				return new Vector3(position.x, boardY, position.z);
			}

			return position + direction * ((boardY - position.y) / direction.y);
		}

		#endregion

		/// <summary>The board's bounds: its tiles, plus the soil below and the grass and mower above.</summary>
		private void GetBoardBounds(Level level, out Vector3 min, out Vector3 max)
		{
			min = positioner.GetWorldPosition(new GridVector(0, 0));
			max = positioner.GetWorldPosition(new GridVector(level.Width - 1, level.Depth - 1));
			float half = LevelDimensions.TileSize * 0.5f + boundsPadding;
			min = new Vector3(min.x - half, min.y - boundsBelow, min.z - half);
			max = new Vector3(max.x + half, max.y + boundsAbove, max.z + half);
		}

		/// <summary>
		/// The camera that fits the board's bounds into <paramref name="target"/> (screen pixels), as (distance back
		/// along the view, sideways, lift) from the bounds' centre.
		/// </summary>
		private Vector3 Solve(Vector3 min, Vector3 max, Quaternion rotation, Rect target)
		{
			Vector3 centre = (min + max) * 0.5f;

			Vector3 right = rotation * Vector3.right;
			Vector3 up = rotation * Vector3.up;
			Vector3 forward = rotation * Vector3.forward;

			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z) - centre;
				_corners[i] = new Vector3(Vector3.Dot(corner, right), Vector3.Dot(corner, up), Vector3.Dot(corner, forward));
			}

			float tanV = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
			float tanH = tanV * Screen.width / Screen.height;

			// Target rectangle in normalised device coordinates (-1..1).
			float tx0 = target.xMin / Screen.width * 2 - 1, tx1 = target.xMax / Screen.width * 2 - 1;
			float ty0 = target.yMin / Screen.height * 2 - 1, ty1 = target.yMax / Screen.height * 2 - 1;

			float distance = 50, sideways = 0, lift = 0;
			for (int iteration = 0; iteration < 24; iteration++)
			{
				float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
				foreach (Vector3 c in _corners)
				{
					float depth = Mathf.Max(0.01f, c.z + distance);
					float x = (c.x - sideways) / (depth * tanH);
					float y = (c.y - lift) / (depth * tanV);
					x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
					y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
				}

				float scale = Mathf.Max((x1 - x0) / Mathf.Max(0.01f, tx1 - tx0), (y1 - y0) / Mathf.Max(0.01f, ty1 - ty0));
				sideways += ((x0 + x1) * 0.5f - (tx0 + tx1) * 0.5f) * distance * tanH;
				lift += ((y0 + y1) * 0.5f - (ty0 + ty1) * 0.5f) * distance * tanV;
				distance *= scale;
			}

			return new Vector3(distance, sideways, lift);
		}
	}
}
