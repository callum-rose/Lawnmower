using System;
using BunnyHeaven.UI;
using UnityEngine;

namespace Game.UI.Screens
{
	/// <summary>
	/// The on-screen arrows for the Arrows movement scheme: four kit round buttons in a cross. Raises the pressed
	/// arrow's screen direction; <see cref="Game.Input.ArrowsMoveInput"/> maps it onto the board. Passive.
	/// </summary>
	internal class ArrowPadView : MonoBehaviour
	{
		[SerializeField] private ButtonView up;
		[SerializeField] private ButtonView down;
		[SerializeField] private ButtonView left;
		[SerializeField] private ButtonView right;

		/// <summary>Screen direction of the pressed arrow (up = (0, 1)).</summary>
		public event Action<Vector2> Pressed;

		public RectTransform Rect => (RectTransform)transform;

		private void Awake()
		{
			up.Clicked += () => Pressed?.Invoke(Vector2.up);
			down.Clicked += () => Pressed?.Invoke(Vector2.down);
			left.Clicked += () => Pressed?.Invoke(Vector2.left);
			right.Clicked += () => Pressed?.Invoke(Vector2.right);
		}

		/// <summary>For tests: what a tap on the arrow does.</summary>
		internal void Press(Vector2 direction) => Pressed?.Invoke(direction);
	}
}
