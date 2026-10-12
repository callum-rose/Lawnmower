using UnityEngine;

namespace Core
{
	/// <summary>Shows a <see cref="Vector2"/> as a min/max slider between <see cref="Min"/> and <see cref="Max"/>.</summary>
	public class MinMaxRangeAttribute : PropertyAttribute
	{
		public float Min { get; }
		public float Max { get; }

		public MinMaxRangeAttribute(float min, float max)
		{
			Min = min;
			Max = max;
		}
	}
}
