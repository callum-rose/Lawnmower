using System;
using System.Reflection;
using UnityEditor;

namespace Lawnmower.Screens.Editor
{
	/// <summary>Sets the Game view to a fixed resolution (e.g. the 390x844 Figma frame) for screenshots. Uses internal editor API.</summary>
	public static class GameViewResolution
	{
		[MenuItem("Bunny Heaven/Screens/Game View 390x844")]
		public static void SetFigmaFrame() => Set(390, 844);

		public static string Set(int width, int height)
		{
			Assembly editor = typeof(EditorWindow).Assembly;
			Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
			Type sizeType = editor.GetType("UnityEditor.GameViewSize");
			Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
			Type gameViewType = editor.GetType("UnityEditor.GameView");

			object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
			object groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
			object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { groupType });
			Type groupClass = group.GetType();

			int total = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null);
			int index = -1;
			for (int i = 0; i < total; i++)
			{
				object size = groupClass.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
				if ((int)sizeType.GetProperty("width").GetValue(size) == width
				    && (int)sizeType.GetProperty("height").GetValue(size) == height
				    && sizeType.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution")
				{
					index = i;
					break;
				}
			}

			if (index < 0)
			{
				object fixedKind = Enum.Parse(sizeKind, "FixedResolution");
				object newSize = Activator.CreateInstance(sizeType, fixedKind, width, height, $"Bunny Heaven {width}x{height}");
				groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
				index = (int)groupClass.GetMethod("GetTotalCount").Invoke(group, null) - 1;
			}

			EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
			PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (selected != null)
			{
				selected.SetValue(gameView, index);
			}
			else
			{
				gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.Invoke(gameView, new object[] { index, null });
			}

			gameView.Repaint();
			return $"Game view size index {index} ({width}x{height})";
		}
	}
}
