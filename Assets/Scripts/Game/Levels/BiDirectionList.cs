using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Levels
{
	public class BiDirectionList<T> : IEnumerable<T>
	{
		public int NegativeCount => -_negativeList.Count;
		public int PositiveCount => _positiveList.Count;

		public int Span => _positiveList.Count + _negativeList.Count;

		public T this[int i]
		{
			get
			{
				if (i >= 0)
				{
					if (i >= _positiveList.Count)
					{
						throw new IndexOutOfRangeException();
					}
					
					return _positiveList[i];
				}
				else
				{
					int negIndex = GetIndexForNegative(i);
					
					if(negIndex >= _negativeList.Count)
					{
						throw new IndexOutOfRangeException();
					}

					return _negativeList[negIndex];
				}
			}
			set
			{
				if (i >= 0)
				{
					_positiveList[i] = value;
				}
				else
				{
					_negativeList[GetIndexForNegative(i)] = value;
				}
			}
		}

		private List<T> _negativeList;
		private List<T> _positiveList;

		public BiDirectionList()
		{
			_negativeList = new List<T>();
			_positiveList = new List<T>();
		}

		public void AddToPositiveSide(T item)
		{
			_positiveList.Add(item);
		}

		public void AddToNegativeSide(T item)
		{
			_negativeList.Add(item);
		}

		public void RemoveAt(int index)
		{
			if (index >= 0)
			{
				_positiveList.RemoveAt(index);
			}
			else
			{
				_negativeList.RemoveAt(GetIndexForNegative(index));
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = NegativeCount; i < PositiveCount; i++)
			{
				yield return this[i];
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private static int GetIndexForNegative(int i)
		{
			return -1 - i;
		}

		public override string ToString()
		{
			return $"-: {NegativeCount}, +:{PositiveCount}";
		}
	}
}