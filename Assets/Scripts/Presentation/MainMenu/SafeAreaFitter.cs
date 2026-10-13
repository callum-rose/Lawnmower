using UnityEngine;

namespace MainMenu
{
	/// <summary>Fits this RectTransform to <see cref="Screen.safeArea"/> (notch, Dynamic Island, home indicator).</summary>
	[RequireComponent(typeof(RectTransform))]
	public class SafeAreaFitter : MonoBehaviour
	{
		private Rect _applied;
		private Vector2Int _screen;

		private void OnEnable() => Apply();

		private void Update()
		{
			if (_applied != Screen.safeArea || _screen.x != Screen.width || _screen.y != Screen.height)
			{
				Apply();
			}
		}

		private void Apply()
		{
			Rect safe = Screen.safeArea;
			_applied = safe;
			_screen = new Vector2Int(Screen.width, Screen.height);

			if (Screen.width <= 0 || Screen.height <= 0)
			{
				return;
			}

			var rect = (RectTransform) transform;
			rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
			rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
			rect.offsetMin = rect.offsetMax = Vector2.zero;
		}
	}
}
