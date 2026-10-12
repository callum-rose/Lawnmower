using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using G = Game.Core.GestureForwardingHandler.Gesture;
using Pair = Game.Core.GestureForwardingHandler.GesturePair;

namespace Tests.EditMode
{
	public class GestureForwardingHandlerTests
	{
		private static readonly List<Pair> Pairs = new List<Pair>
		{
			new Pair { a = G.Pinch, b = G.Twist },
			new Pair { a = G.Pinch, b = G.TwoFingerDrag },
			new Pair { a = G.Twist, b = G.TwoFingerDrag }
		};

		[TestCase((int)G.Pinch, (int)G.Twist, true)]
		[TestCase((int)G.Twist, (int)G.Pinch, true)]
		[TestCase((int)G.TwoFingerDrag, (int)G.Twist, true)]
		[TestCase((int)G.Swipe, (int)G.Swipe, true)]
		[TestCase((int)G.None, (int)G.Tap, true)]
		[TestCase((int)G.Swipe, (int)G.Tap, false)]
		[TestCase((int)G.Pinch, (int)G.Swipe, false)]
		[TestCase((int)G.Tap, (int)G.TwoFingerDrag, false)]
		public void AreAllowedTogether_MatchesMigratedMatrix(int first, int second, bool expected)
		{
			Assert.AreEqual(expected, GestureForwardingHandler.AreAllowedTogether(Pairs, (G)first, (G)second));
		}
	}
}
