using UnityEngine;

namespace Lawnmower.Screens
{
	/// <summary>
	/// Fits this RectTransform (a full-screen child of a canvas) to <see cref="Screen.safeArea"/>, so content clears the
	/// notch / Dynamic Island and the home indicator. Minimum insets (canvas units) keep the Figma spacing on screens
	/// without a notch, where the design's 54pt status-bar strip would otherwise vanish.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaPanel : MonoBehaviour
	{
		[SerializeField, Min(0)] private float minTop = 48f;
		[SerializeField, Min(0)] private float minBottom;

		private Rect _appliedArea;
		private Vector2Int _appliedScreen;
		private Canvas _canvas;

		private void OnEnable() => Apply();

		private void Update()
		{
			if (Screen.safeArea != _appliedArea || _appliedScreen.x != Screen.width || _appliedScreen.y != Screen.height)
			{
				Apply();
			}
		}

		private void Apply()
		{
			RectTransform rect = (RectTransform)transform;
			_canvas = _canvas != null ? _canvas : GetComponentInParent<Canvas>();
			_appliedArea = Screen.safeArea;
			_appliedScreen = new Vector2Int(Screen.width, Screen.height);

			if (Screen.width <= 0 || Screen.height <= 0)
			{
				return;
			}

			float scale = _canvas != null && _canvas.rootCanvas.scaleFactor > 0f ? _canvas.rootCanvas.scaleFactor : 1f;
			float top = Mathf.Max(minTop, (Screen.height - _appliedArea.yMax) / scale);
			float bottom = Mathf.Max(minBottom, _appliedArea.yMin / scale);
			float left = _appliedArea.xMin / scale;
			float right = (Screen.width - _appliedArea.xMax) / scale;

			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = new Vector2(left, bottom);
			rect.offsetMax = new Vector2(-right, -top);
		}
	}
}
