using System.Collections;
using UnityEngine;

namespace Game.UndoSystem
{
    public struct Xor
    {
        public bool IsTrue { get; private set; }
        public bool IsFalse => !IsTrue;

        public Xor(params bool[] args)
        {
            IsTrue = false;
            foreach (bool arg in args)
            {
                IsTrue ^= arg;
            }
        }

        public static implicit operator bool(Xor inversion)
        {
            return inversion.IsTrue;
        }

        public static explicit operator Xor(bool b)
        {
            return new Xor(b);
        }

        public static Xor operator ^(Xor i1, Xor i2)
        {
            return (Xor)(i1.IsTrue ^ i2.IsTrue);
        }

        public static Xor operator ^(bool b, Xor i)
        {
            return (Xor)(b ^ i.IsTrue);
        }

        public static Xor operator ^(Xor i, bool b)
        {
            return b ^ i;
        }

        public static Xor Combine(params bool[] args)
        {
            return new Xor(args);
        }

        public override string ToString()
        {
            return ((bool)this).ToString();
        }
    }
}
