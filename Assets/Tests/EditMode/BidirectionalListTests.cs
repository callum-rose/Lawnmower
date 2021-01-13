using System;
using System.Collections;
using Game.Levels;
using Moq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
	public class BidirectionalListTests
	{
		[Test]
		public void EmptyListHasSpanOfZero()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			Assert.AreEqual(0, list.Span);
		}

		[Test]
		public void EmptyListHasNegativeCountOfZero()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			Assert.AreEqual(0, list.NegativeCount);
		}

		[Test]
		public void EmptyListHasPositiveCountOfZero()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			Assert.AreEqual(0, list.PositiveCount);
		}

		[Test]
		public void CanAddToPositiveSideOfList()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			Assert.DoesNotThrow(() => list.AddToPositiveSide(It.IsAny<It.IsAnyType>()));
		}

		[Test]
		public void CanAddToNegativeSideOfList()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			Assert.DoesNotThrow(() => list.AddToNegativeSide(It.IsAny<It.IsAnyType>()));
		}

		[Test]
		public void AddingElementToPositiveSideOfListReturnsPositiveCountOfOne()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());

			Assert.AreEqual(1, list.PositiveCount);
		}
		
		[Test]
		public void AddingTwoElementsToPositiveSideOfListReturnsPositiveCountOfTwo()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());

			Assert.AreEqual(2, list.PositiveCount);
		}

		[Test]
		public void AddingElementToNegativeSideOfListReturnsNegativeCountOfMinusOne()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToNegativeSide(It.IsAny<It.IsAnyType>());

			Assert.AreEqual(-1, list.NegativeCount);
		}

		[Test]
		public void GettingElementAbovePositiveCountThrows()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());

			Assert.Throws<IndexOutOfRangeException>(() =>
			{
				It.IsAnyType temp = list[list.PositiveCount + 1];
			});
		}

		[Test]
		public void GettingElementBelowNegativeCountThrows()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToNegativeSide(It.IsAny<It.IsAnyType>());

			Assert.Throws<IndexOutOfRangeException>(() =>
			{
				It.IsAnyType temp = list[list.NegativeCount - 1];
			});
		}

		[Test]
		public void GetZeroIndexReturnsInputValue()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();

			It.IsAnyType itemAt0 = It.IsAny<It.IsAnyType>();
			list.AddToPositiveSide(itemAt0);

			Assert.AreSame(itemAt0, list[0]);
		}

		[Test]
		public void GetPositiveIndexReturnsInputValue()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();

			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());

			It.IsAnyType itemAt1 = It.IsAny<It.IsAnyType>();
			list.AddToPositiveSide(itemAt1);

			Assert.AreSame(itemAt1, list[1]);
		}

		[Test]
		public void GetNegativeIndexReturnsInputValue()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();

			It.IsAnyType itemAtM1 = It.IsAny<It.IsAnyType>();
			list.AddToNegativeSide(itemAtM1);

			Assert.AreSame(itemAtM1, list[-1]);
		}

		[Test]
		public void CanSetItemAtPositiveIndex()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());

			It.IsAnyType itemAt1 = new It.IsAnyType();
			Assert.DoesNotThrow(() => list[1] = itemAt1);
			Assert.AreSame(itemAt1, list[1]);
		}

		[Test]
		public void CanSetItemAtNegativeIndex()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToNegativeSide(It.IsAny<It.IsAnyType>());
			list.AddToNegativeSide(It.IsAny<It.IsAnyType>());

			It.IsAnyType itemAt1 = new It.IsAnyType();
			Assert.DoesNotThrow(() => list[-2] = itemAt1);
			Assert.AreSame(itemAt1, list[-2]);
		}

		[Test]
		public void CanSetRangeOfItems()
		{
			BiDirectionList<It.IsAnyType> list = new BiDirectionList<It.IsAnyType>();
			list.AddToNegativeSide(It.IsAny<It.IsAnyType>());
			list.AddToNegativeSide(It.IsAny<It.IsAnyType>());
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());
			list.AddToPositiveSide(It.IsAny<It.IsAnyType>());

			for (int i = list.NegativeCount + 1; i < list.PositiveCount; i++)
			{
				Assert.DoesNotThrow(() => list[i] = new It.IsAnyType());
			}
		}

		[Test]
		public void CanGetRangeOfItems()
		{
			BiDirectionList<int> list = new BiDirectionList<int>();

			int[] anyArr =
			{
				1, 2, 3, 4, 5
			};
			
			list.AddToPositiveSide(anyArr[2]);
			list.AddToPositiveSide(anyArr[3]);
			list.AddToPositiveSide(anyArr[4]);
			list.AddToNegativeSide(anyArr[1]);
			list.AddToNegativeSide(anyArr[0]);

			int j = 0;
			for (int i = list.NegativeCount; i < list.PositiveCount; i++)
			{
				int anyIn = anyArr[j++];
				int anyOut = list[i];
				Assert.AreEqual(anyIn, anyOut);
			}
		}

		[Test]
		public void EnumeratorReturnsValuesFromLowToHigh()
		{
			BiDirectionList<int> list = new BiDirectionList<int>();

			int[] anyArr =
			{
				1, 2, 3, 4, 5
			};
			
			list.AddToPositiveSide(anyArr[2]);
			list.AddToPositiveSide(anyArr[3]);
			list.AddToPositiveSide(anyArr[4]);
			list.AddToNegativeSide(anyArr[1]);
			list.AddToNegativeSide(anyArr[0]);

			int j = 0;
			foreach (int item in list)
			{
				Assert.AreEqual(anyArr[j++], item);
			}
		}
	}
}