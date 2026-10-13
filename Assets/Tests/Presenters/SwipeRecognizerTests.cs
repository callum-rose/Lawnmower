using System;
using Game.Core;
using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class SwipeRecognizerTests
	{
		private const float MinDistance = 50;

		private SwipeRecognizer _recognizer;

		[SetUp]
		public void SetUp()
		{
			_recognizer = new SwipeRecognizer(MinDistance, 0.8f);
		}

		[Test]
		public void ShortMove_IsNotASwipe()
		{
			_recognizer.Begin(100, 100);

			Assert.IsFalse(_recognizer.Move(140, 100, out _));
			Assert.IsTrue(_recognizer.IsTracking);
		}

		[Test]
		public void PassingTheDistance_IsASwipe_FromTheStart()
		{
			_recognizer.Begin(100, 100);
			_recognizer.Move(130, 101, out _);

			Assert.IsTrue(_recognizer.Move(160, 102, out Swipe swipe));
			Assert.AreEqual(100, swipe.StartX);
			Assert.AreEqual(100, swipe.StartY);
			Assert.AreEqual(60, swipe.DeltaX);
			Assert.AreEqual(2, swipe.DeltaY);
		}

		[Test]
		public void OneSwipePerTouch()
		{
			_recognizer.Begin(0, 0);
			Assert.IsTrue(_recognizer.Move(0, 60, out _));

			Assert.IsFalse(_recognizer.Move(0, 200, out _));
			Assert.IsFalse(_recognizer.Move(200, 200, out _));
			Assert.IsFalse(_recognizer.IsTracking);
		}

		[Test]
		public void NextTouch_CanSwipeAgain()
		{
			_recognizer.Begin(0, 0);
			_recognizer.Move(0, 60, out _);
			_recognizer.End();

			_recognizer.Begin(0, 0);
			Assert.IsTrue(_recognizer.Move(-60, 0, out Swipe swipe));
			Assert.AreEqual(-60, swipe.DeltaX);
		}

		[Test]
		public void WigglyPath_IsRejected_ForTheWholeTouch()
		{
			_recognizer.Begin(0, 0);
			_recognizer.Move(40, 0, out _);
			_recognizer.Move(0, 0, out _);
			_recognizer.Move(40, 0, out _);

			// 55 px away after 135 px of travel: 0.41 straight.
			Assert.IsFalse(_recognizer.Move(55, 0, out _));
			Assert.IsFalse(_recognizer.IsTracking);
			Assert.IsFalse(_recognizer.Move(300, 0, out _));
		}

		[Test]
		public void SlightlyCurvedPath_StillSwipes()
		{
			_recognizer.Begin(0, 0);
			_recognizer.Move(20, 8, out _);
			_recognizer.Move(40, 10, out _);

			Assert.IsTrue(_recognizer.Move(60, 8, out _));
		}

		[Test]
		public void MoveWithoutBegin_OrAfterEnd_IsIgnored()
		{
			Assert.IsFalse(_recognizer.Move(500, 0, out _));

			_recognizer.Begin(0, 0);
			_recognizer.End();
			Assert.IsFalse(_recognizer.Move(500, 0, out _));
		}

		[TestCase(10, 2, 1, 0)]
		[TestCase(-10, 2, -1, 0)]
		[TestCase(2, 10, 0, 1)]
		[TestCase(2, -10, 0, -1)]
		[TestCase(5, 5, 0, 1)]
		[TestCase(0, 0, 0, 0)]
		public void ToCardinal_TakesTheDominantAxis(float dx, float dy, int x, int y)
		{
			Assert.AreEqual(new GridVector(x, y), SwipeRecognizer.ToCardinal(dx, dy));
		}

		[TestCase(0f, 0.8f)]
		[TestCase(-1f, 0.8f)]
		[TestCase(10f, 1.5f)]
		[TestCase(10f, -0.1f)]
		public void InvalidSettings_Throw(float minDistance, float straightness)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new SwipeRecognizer(minDistance, straightness));
		}
	}
}
