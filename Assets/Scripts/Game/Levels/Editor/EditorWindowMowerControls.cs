#if UNITY_EDITOR
using System;
using Game.Core;
using Game.Mowers.Input;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Game.Levels.EditorWindow
{
	internal class EditorWindowMowerControlsDrawer : OdinValueDrawer<EditorWindowMowerControls>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			EditorWindowMowerControls controls = ValueEntry.SmartValue;

			Rect fullRect = GUILayoutUtility.GetRect(0, 99999, 150, 100);
			Rect midSquareRect = new Rect(fullRect.center - Vector2.one * fullRect.height / 2,
				Vector2.one * fullRect.height);

			float columnWidth = midSquareRect.width / 3;
			for (int i = 0; i < 9; i++)
			{
				Rect ButtonRect() => midSquareRect.SplitTableGrid(3, columnWidth, i);
				switch (i)
				{
					case 1:
						DrawButton(ButtonRect(), "Up", () => controls.Move(GridVector.Up));
						break;		
					case 3:
						DrawButton(ButtonRect(), "Left", () => controls.Move(GridVector.Left));
						break;	
					case 5:
						DrawButton(ButtonRect(), "Right", () => controls.Move(GridVector.Right));
						break;	
					case 7:
						DrawButton(ButtonRect(), "Down", () => controls.Move(GridVector.Down));
						break;
				}
			}

			if (Event.current.OnKeyDown(KeyCode.RightArrow) || Event.current.OnKeyDown(KeyCode.D))
			{
				controls.Move(GridVector.Right);
			}
			else if (Event.current.OnKeyDown(KeyCode.DownArrow) || Event.current.OnKeyDown(KeyCode.S))
			{
				controls.Move(GridVector.Down);
			}
			else if (Event.current.OnKeyDown(KeyCode.LeftArrow) || Event.current.OnKeyDown(KeyCode.A))
			{
				controls.Move(GridVector.Left);
			}
			else if (Event.current.OnKeyDown(KeyCode.UpArrow) || Event.current.OnKeyDown(KeyCode.W))
			{
				controls.Move(GridVector.Up);
			}
		}

		private static void DrawButton(Rect rect, string text, Action action)
		{
			if (GUI.Button(rect, text))
			{
				action.Invoke();
			}
		}
	}
	
	internal class EditorWindowMowerControls : IMowerControls
	{
		public MowerInputEventChannel mowerInputEventChannel;

		public void Move(GridVector direction)
		{
			mowerInputEventChannel.Raise(direction);
		}
	}
}
#endif