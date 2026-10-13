using Game.Core;
using Game.Levels;
using Game.UI.Screens;
using Lawnmower.Model;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Cameras
{
	/// <summary>
	/// Frames the whole board into the screen rectangle the HUD leaves free (<see cref="GameScreenView.GetBoardScreenRect"/>):
	/// a fixed-angle camera is moved (distance and sideways) so the board's bounds just fit, for any level size,
	/// aspect and safe area. Drives a high-priority, procedural-free <see cref="CinemachineCamera"/>, so the brain
	/// renders it over the old follow/level rigs.
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

		private Level _framedLevel;
		private Vector3 _velocity;
		private readonly Vector3[] _corners = new Vector3[8];

		public Rect LastTarget { get; private set; }

		private void LateUpdate()
		{
			Level level = levelManager.Level;
			if (level == null || boardCamera == null || Screen.width <= 0 || Screen.height <= 0)
			{
				_framedLevel = null;
				return;
			}

			Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
			Rect target = screen != null ? screen.GetBoardScreenRect() : new Rect(0, 0, Screen.width, Screen.height);
			LastTarget = target;
			Vector3 position = Solve(level, rotation, target);

			LensSettings lens = boardCamera.Lens;
			lens.FieldOfView = fieldOfView;
			lens.NearClipPlane = 10;
			lens.FarClipPlane = 600;
			boardCamera.Lens = lens;
			boardCamera.transform.rotation = rotation;

			if (_framedLevel != level || smoothTime <= 0)
			{
				_framedLevel = level;
				_velocity = Vector3.zero;
				boardCamera.transform.position = position;
				boardCamera.PreviousStateIsValid = false;
				return;
			}

			boardCamera.transform.position = Vector3.SmoothDamp(boardCamera.transform.position, position, ref _velocity,
				smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
		}

		/// <summary>The camera position that fits the board's bounds into <paramref name="target"/> (screen pixels).</summary>
		private Vector3 Solve(Level level, Quaternion rotation, Rect target)
		{
			Vector3 min = positioner.GetWorldPosition(new GridVector(0, 0));
			Vector3 max = positioner.GetWorldPosition(new GridVector(level.Width - 1, level.Depth - 1));
			float half = LevelDimensions.TileSize * 0.5f + boundsPadding;
			min = new Vector3(min.x - half, min.y - boundsBelow, min.z - half);
			max = new Vector3(max.x + half, max.y + boundsAbove, max.z + half);
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

			return centre - forward * distance + right * sideways + up * lift;
		}
	}
}
