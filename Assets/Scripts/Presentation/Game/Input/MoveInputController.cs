using System;
using Game.Core;
using Game.UI.Screens;
using Lawnmower.Presenters;
using UnityEngine;

namespace Game.Input
{
	/// <summary>
	/// The Game scene's move input on the New Input System: swipes or the on-screen arrows (whichever
	/// <see cref="MovementSchemeSetting"/> selects), plus the keyboard for editor testing. <see cref="GameManager"/>
	/// attaches <see cref="Input"/> to each level's presenter.
	/// </summary>
	[DefaultExecutionOrder(-50)]
	internal class MoveInputController : MonoBehaviour
	{
		[SerializeField] private ScreenToWorldPointConverter screenToBoard;
		[Tooltip("The on-screen arrows; shown only while the Arrows scheme is active.")]
		[SerializeField] private ArrowPadView arrows;

		[Header("Swipe")]
		[SerializeField, Min(0.01f)] private float swipeMinDistanceInches = 0.2f;
		[SerializeField, Range(0, 1)] private float swipeMinStraightness = 0.8f;

		private KeyboardMoveInput _keyboard;
		private SwipeMoveInput _swipe;
		private ArrowsMoveInput _arrows;
		private SwitchableMoveInput _input;

		/// <summary>Every move the player asks for, whatever the source. Not raised from simulation events.</summary>
		public event Action<GridVector> MoveRequested;

		public IMoveInput Input
		{
			get
			{
				EnsureCreated();
				return _input;
			}
		}

		public ArrowPadView Arrows => arrows;

		public MovementScheme Scheme
		{
			get => Input is SwitchableMoveInput s ? s.Scheme : MovementSchemeSetting.Current;
			set => MovementSchemeSetting.Current = value;
		}

		#region Unity

		private void Awake()
		{
			EnsureCreated();
		}

		private void OnEnable()
		{
			MovementSchemeSetting.Changed += ApplyScheme;
			ApplyScheme(MovementSchemeSetting.Current);
		}

		private void OnDisable()
		{
			MovementSchemeSetting.Changed -= ApplyScheme;
		}

		private void Update()
		{
			_keyboard.Tick();
			_swipe.Tick();
		}

		private void OnDestroy()
		{
			if (_input == null)
			{
				return;
			}

			_input.MoveRequested -= OnMoveRequested;
			_input.Dispose();
			_arrows.Dispose();
		}

		#endregion

		#region Methods

		private void EnsureCreated()
		{
			if (_input != null)
			{
				return;
			}

			_keyboard = new KeyboardMoveInput();
			_swipe = new SwipeMoveInput(screenToBoard, swipeMinDistanceInches, swipeMinStraightness);
			_arrows = new ArrowsMoveInput(arrows, screenToBoard);
			_input = new SwitchableMoveInput(_swipe, _arrows, MovementSchemeSetting.Current, _keyboard);
			_input.MoveRequested += OnMoveRequested;
		}

		private void ApplyScheme(MovementScheme scheme)
		{
			EnsureCreated();
			_input.Scheme = scheme;
			_swipe.Reset();

			if (arrows != null)
			{
				arrows.gameObject.SetActive(scheme == MovementScheme.Arrows);
			}
		}

		private void OnMoveRequested(GridVector direction) => MoveRequested?.Invoke(direction);

		#endregion
	}
}
