using System;

namespace BalsamicBits.Extensions
{
    public static class EnumExtensions
    {
        public static T[] GetValues<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }

        public static int ValueCount<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T)).Length;
        }
    }
}
