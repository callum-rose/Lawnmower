using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    [Serializable]
    public class Serialised2dArray<T> : IEnumerable<T>
    {
        [SerializeField,
            ListDrawerSettings(Expanded = true, HideAddButton = true, HideRemoveButton = true, ShowIndexLabels = true, ShowItemCount = true, ShowPaging = false)]
        private T[] data;

        [SerializeField, HideInInspector] private int width, depth;

        public int Width => width;
        public int Depth => depth;

        public T this[int x, int y]
        {
            get => GetElement(x, y);
            set => SetElement(x, y, value);
        }

        public Serialised2dArray(int width, int depth)
        {
            this.width = width;
            this.depth = depth;

            data = new T[this.width * this.Depth];
        }

        public Serialised2dArray(int width, int depth, T[] data)
        {
            this.width = width;
            this.depth = depth;

            this.data = data.ToArray();
        }

        public Serialised2dArray(int width, int depth, T[,] data)
        {
            this.width = width;
            this.depth = depth;

            this.data = new T[this.width * this.Depth];
            Utils.Loops.TwoD(width, depth, (x, y) =>
            {
                this[x, y] = data[x, y];
            });
        }

        public Serialised2dArray(Serialised2dArray<T> toCopy)
        {
            width = toCopy.width;
            depth = toCopy.Depth;

            data = toCopy.data.ToArray();
        }

        #region API

        public IEnumerator<T> GetEnumerator()
        {
            foreach (T d in data)
            {
                yield return d;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Methods

        private void SetElement(int x, int y, T value)
        {
            AssertInRange(x, y);

            int index = GetIndexFromPos(x, y);
            data[index] = value;
        }

        private T GetElement(int x, int y)
        {
            AssertInRange(x, y);

            int index = GetIndexFromPos(x, y);
            return data[index];
        }

        private int GetIndexFromPos(int x, int y)
        {
            return y * Width + x;
        }

        private void AssertInRange(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Depth)
            {
                throw new IndexOutOfRangeException($"x: {x}, y: {y}, width: {Width}, depth: {Depth}");
            }
        }

        #endregion
    }
}
