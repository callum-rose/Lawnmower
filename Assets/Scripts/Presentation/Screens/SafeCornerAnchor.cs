using UnityEngine;

namespace Lawnmower.Screens
{
	/// <summary>
	/// Pins this RectTransform (a child of a full-screen rect) into the screen's top-right corner. With a central notch or
	/// Dynamic Island, it sits in the true corner beside the cutout, clear of the display's rounded corner; otherwise it
	/// sits a margin inside the safe area.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	public class SafeCornerAnchor : MonoBehaviour
	{
		[Tooltip("Gap from the safe area's edges when there's no central cutout (canvas units).")]
		[SerializeField, Min(0)] private float margin = 16f;

		[Tooltip("The display's corner radius on notched phones (canvas units ≈ points).")]
		[SerializeField, Min(0)] private float cornerRadius = 55f;

		[Tooltip("Gap kept from the rounded corner and from the cutout (canvas units).")]
		[SerializeField, Min(0)] private float cornerGap = 6f;

		[Tooltip("A top inset at least this big (canvas units) means a notch or Dynamic Island when the platform reports no cutouts.")]
		[SerializeField, Min(0)] private float notchInset = 40f;

		private Rect _appliedArea;
		private Vector2Int _appliedScreen;
		private Vector2 _appliedSize;
		private Canvas _canvas;

		private void OnEnable() => Apply();

		private void Update()
		{
			var rect = (RectTransform)transform;
			if (Screen.safeArea != _appliedArea || _appliedScreen.x != Screen.width || _appliedScreen.y != Screen.height ||
			    rect.rect.size != _appliedSize)
			{
				Apply();
			}
		}

		private void Apply()
		{
			var rect = (RectTransform)transform;
			_canvas = _canvas != null ? _canvas : GetComponentInParent<Canvas>();
			_appliedArea = Screen.safeArea;
			_appliedScreen = new Vector2Int(Screen.width, Screen.height);
			_appliedSize = rect.rect.size;

			rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;

			if (Screen.width <= 0 || Screen.height <= 0)
			{
				return;
			}

			float scale = _canvas != null && _canvas.rootCanvas.scaleFactor > 0f ? _canvas.rootCanvas.scaleFactor : 1f;
			float top = (Screen.height - _appliedArea.yMax) / scale;
			float right = (Screen.width - _appliedArea.xMax) / scale;
			float radius = Mathf.Max(_appliedSize.x, _appliedSize.y) / 2f;

			// Inside the rounded corner: the circle's centre sits on the corner's diagonal, far enough in to clear the curve.
			float centre = Mathf.Max(radius + cornerGap, cornerRadius - (cornerRadius - radius - cornerGap) / Mathf.Sqrt(2f));
			if (TryGetCentralCutoutRight(scale, out float cutoutRight) && centre + radius + cornerGap <= cutoutRight)
			{
				float inset = centre - radius;
				rect.anchoredPosition = new Vector2(-inset, -inset);
				return;
			}

			rect.anchoredPosition = new Vector2(-(right + margin), -(top + margin));
		}

		/// <summary>The distance (canvas units) from the screen's right edge to a central top cutout's right edge.</summary>
		private bool TryGetCentralCutoutRight(float scale, out float distance)
		{
			distance = 0f;
			bool found = false;
			foreach (Rect cutout in Screen.cutouts)
			{
				bool atTop = cutout.yMax >= Screen.height - 1;
				bool central = Mathf.Abs(cutout.center.x - Screen.width / 2f) < Screen.width / 6f;
				if (atTop && central)
				{
					distance = (Screen.width - cutout.xMax) / scale;
					found = true;
				}
			}

			if (found)
			{
				return true;
			}

			// No cutouts reported: a tall top inset on a portrait phone is a central notch or Dynamic Island. The widest
			// notch (iPhone X–12) spans about 54% of the width.
			float top = (Screen.height - Screen.safeArea.yMax) / scale;
			if (Screen.cutouts.Length == 0 && top >= notchInset)
			{
				distance = Screen.width / scale * 0.23f;
				return true;
			}

			return false;
		}
	}
}
