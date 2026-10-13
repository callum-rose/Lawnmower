using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace Game.Input
{
	/// <summary>Queries on the touchscreen shared by the swipe input and the board camera's two-finger gestures.</summary>
	internal static class ScreenTouches
	{
		private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

		/// <summary>How many fingers are on the screen; 0 without a touchscreen.</summary>
		public static int CountPressed()
		{
			Touchscreen touchscreen = Touchscreen.current;
			if (touchscreen == null)
			{
				return 0;
			}

			int count = 0;
			foreach (TouchControl touch in touchscreen.touches)
			{
				if (touch.press.isPressed)
				{
					count++;
				}
			}

			return count;
		}

		/// <summary>Only canvas UI counts: the board itself is hit by the camera's physics raycaster.</summary>
		public static bool IsOverCanvas(Vector2 screenPosition)
		{
			EventSystem eventSystem = EventSystem.current;
			if (eventSystem == null)
			{
				return false;
			}

			RaycastResults.Clear();
			eventSystem.RaycastAll(new PointerEventData(eventSystem) { position = screenPosition }, RaycastResults);

			foreach (RaycastResult result in RaycastResults)
			{
				if (result.module is GraphicRaycaster)
				{
					return true;
				}
			}

			return false;
		}
	}
}
