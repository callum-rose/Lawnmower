using UnityEngine;
using UnityEngine.UI;

namespace MainMenu
{
	/// <summary>Shows a camera's view in a RawImage, at the image's on-screen pixel size.</summary>
	[RequireComponent(typeof(RawImage))]
	public class CameraRawImage : MonoBehaviour
	{
		[SerializeField] private Camera sourceCamera;

		private static readonly Vector3[] corners = new Vector3[4];

		private RawImage _image;
		private RenderTexture _texture;

		#region Unity

		private void Awake()
		{
			_image = GetComponent<RawImage>();
		}

		private void LateUpdate()
		{
			Vector2Int size = GetPixelSize();

			if (_texture != null && _texture.width == size.x && _texture.height == size.y)
			{
				return;
			}

			ReleaseTexture();

			_texture = new RenderTexture(size.x, size.y, 24) { name = $"{name} ({sourceCamera.name})", antiAliasing = 4 };
			sourceCamera.targetTexture = _texture;
			sourceCamera.aspect = (float)size.x / size.y;
			_image.texture = _texture;
		}

		private void OnDestroy()
		{
			ReleaseTexture();
		}

		#endregion

		public Camera SourceCamera => sourceCamera;

		/// <summary>Converts a screen point into the source camera's viewport space (0..1 inside the image).</summary>
		public Vector2 ScreenToViewportPoint(Vector2 screenPoint)
		{
			Canvas canvas = _image.canvas.rootCanvas;
			Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
			var rectTransform = (RectTransform)transform;
			Rect rect = rectTransform.rect;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, canvasCamera, out Vector2 local);
			return (local - rect.min) / rect.size;
		}

		private Vector2Int GetPixelSize()
		{
			Canvas canvas = _image.canvas.rootCanvas;
			Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

			((RectTransform)transform).GetWorldCorners(corners);
			Vector2 min = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[0]);
			Vector2 max = RectTransformUtility.WorldToScreenPoint(canvasCamera, corners[2]);

			return new Vector2Int(Mathf.Max(1, Mathf.RoundToInt(max.x - min.x)), Mathf.Max(1, Mathf.RoundToInt(max.y - min.y)));
		}

		private void ReleaseTexture()
		{
			if (_texture == null)
			{
				return;
			}

			if (sourceCamera != null)
			{
				sourceCamera.targetTexture = null;
			}

			_texture.Release();
			Destroy(_texture);
			_texture = null;
		}
	}
}
