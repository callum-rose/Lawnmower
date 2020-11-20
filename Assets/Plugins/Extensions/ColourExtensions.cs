using UnityEngine;

namespace BalsamicBits.Extensions
{
    public static class ColourExtensions
    {
        public static Color SetAlpha (this Color colour, float alpha)
        {
            Color tempCol = colour;
            tempCol.a = alpha;
            return tempCol;
        }
    }
}
