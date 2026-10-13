using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using static Lawnmower.Model.Tests.Sim;

namespace Lawnmower.Model.Tests
{
	public class MoveNotationTests
	{
		[Test]
		public void Parse_ReadsUDLR()
		{
			CollectionAssert.AreEqual(new[] { R, R, U, L }, MoveNotation.Parse("RRUL"));
			CollectionAssert.AreEqual(new[] { D }, MoveNotation.Parse("D"));
		}

		[Test]
		public void Parse_IgnoresWhitespaceAndCase()
		{
			CollectionAssert.AreEqual(new[] { U, D, L, R }, MoveNotation.Parse(" u D\tl\nr "));
		}

		[TestCase("")]
		[TestCase(null)]
		[TestCase("   ")]
		public void Parse_EmptyOrNull_IsNoMoves(string text)
		{
			CollectionAssert.IsEmpty(MoveNotation.Parse(text));
			Assert.IsTrue(MoveNotation.TryParse(text, out IReadOnlyList<GridVector> moves));
			CollectionAssert.IsEmpty(moves);
		}

		[TestCase("RX")]
		[TestCase("1")]
		[TestCase("UP")]
		[TestCase("R,L")]
		public void Parse_InvalidCharacter_Throws_AndTryParseFails(string text)
		{
			Assert.Throws<FormatException>(() => MoveNotation.Parse(text));
			Assert.IsFalse(MoveNotation.TryParse(text, out IReadOnlyList<GridVector> moves));
			Assert.IsNull(moves);
		}

		[TestCase("RRUL")]
		[TestCase("udlr")]
		[TestCase("URURLL")]
		[TestCase("")]
		public void Format_OfParse_IsUpperCaseInput(string text)
		{
			Assert.AreEqual(text.ToUpperInvariant(), MoveNotation.Format(MoveNotation.Parse(text)));
		}

		[TestCase(0, 0)]
		[TestCase(1, 1)]
		[TestCase(2, 0)]
		public void Format_NonCardinal_Throws(int x, int y)
		{
			Assert.Throws<ArgumentException>(() => MoveNotation.Format(new[] { R, new GridVector(x, y) }));
			Assert.Throws<ArgumentException>(() => MoveNotation.ToChar(new GridVector(x, y)));
		}
	}
}
