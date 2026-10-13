using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class SwitchableMoveInputTests
	{
		private FakeInput _swipe;
		private FakeInput _arrows;
		private FakeInput _keyboard;
		private List<GridVector> _moves;

		[SetUp]
		public void SetUp()
		{
			_swipe = new FakeInput();
			_arrows = new FakeInput();
			_keyboard = new FakeInput();
			_moves = new List<GridVector>();
		}

		private SwitchableMoveInput Create(MovementScheme scheme)
		{
			SwitchableMoveInput input = new SwitchableMoveInput(_swipe, _arrows, scheme, _keyboard);
			input.MoveRequested += _moves.Add;
			return input;
		}

		[Test]
		public void Swipe_ForwardsSwipesOnly()
		{
			Create(MovementScheme.Swipe);

			_swipe.Press(GridVector.Up);
			_arrows.Press(GridVector.Left);

			CollectionAssert.AreEqual(new[] { GridVector.Up }, _moves);
		}

		[Test]
		public void Arrows_ForwardsArrowsOnly()
		{
			Create(MovementScheme.Arrows);

			_swipe.Press(GridVector.Up);
			_arrows.Press(GridVector.Left);

			CollectionAssert.AreEqual(new[] { GridVector.Left }, _moves);
		}

		[Test]
		public void AlwaysOnSources_ForwardInEveryScheme()
		{
			SwitchableMoveInput input = Create(MovementScheme.Swipe);
			_keyboard.Press(GridVector.Right);
			input.Scheme = MovementScheme.Arrows;
			_keyboard.Press(GridVector.Down);

			CollectionAssert.AreEqual(new[] { GridVector.Right, GridVector.Down }, _moves);
		}

		[Test]
		public void SwitchingAtRuntime_ChangesTheForwardedSource()
		{
			SwitchableMoveInput input = Create(MovementScheme.Swipe);
			List<MovementScheme> changes = new List<MovementScheme>();
			input.SchemeChanged += changes.Add;

			_swipe.Press(GridVector.Up);
			input.Scheme = MovementScheme.Arrows;
			_swipe.Press(GridVector.Up);
			_arrows.Press(GridVector.Left);
			input.Scheme = MovementScheme.Arrows;
			input.Scheme = MovementScheme.Swipe;
			_arrows.Press(GridVector.Left);
			_swipe.Press(GridVector.Down);

			CollectionAssert.AreEqual(new[] { GridVector.Up, GridVector.Left, GridVector.Down }, _moves);
			CollectionAssert.AreEqual(new[] { MovementScheme.Arrows, MovementScheme.Swipe }, changes, "no event when unchanged");
		}

		[Test]
		public void Dispose_StopsForwarding()
		{
			SwitchableMoveInput input = Create(MovementScheme.Swipe);
			input.Dispose();

			Assert.IsFalse(_swipe.HasListeners);
			Assert.IsFalse(_arrows.HasListeners);
			Assert.IsFalse(_keyboard.HasListeners);
		}

		[Test]
		public void DefaultScheme_IsSwipe()
		{
			Assert.AreEqual(MovementScheme.Swipe, MovementSchemeSetting.Default);
			Assert.AreEqual(MovementScheme.Swipe, new SwitchableMoveInput(_swipe, _arrows).Scheme);
		}

		[Test]
		public void Setting_RaisesChangedOnlyOnChange()
		{
			MovementScheme before = MovementSchemeSetting.Current;
			List<MovementScheme> changes = new List<MovementScheme>();
			MovementSchemeSetting.Changed += changes.Add;

			try
			{
				MovementSchemeSetting.Current = MovementScheme.Swipe;
				MovementSchemeSetting.Current = MovementScheme.Arrows;
				MovementSchemeSetting.Current = MovementScheme.Arrows;
				MovementSchemeSetting.Current = MovementScheme.Swipe;

				int expectedFirst = before == MovementScheme.Swipe ? 0 : 1;
				Assert.AreEqual(expectedFirst + 2, changes.Count);
				Assert.AreEqual(MovementScheme.Swipe, MovementSchemeSetting.Current);
			}
			finally
			{
				MovementSchemeSetting.Changed -= changes.Add;
				MovementSchemeSetting.Current = before;
			}
		}
	}
}
