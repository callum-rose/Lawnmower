using System;

namespace Lawnmower.Presenters
{
	public static class AudioVolume
	{
		/// <summary>The mixer's floor: a slider at 0 mutes.</summary>
		public const float SilentDecibels = -80f;

		/// <summary>A 0–1 slider value to an AudioMixer attenuation in dB (1 → 0 dB, 0.5 → about -6 dB, 0 → -80 dB).</summary>
		public static float ToDecibels(float linear)
		{
			if (float.IsNaN(linear) || linear <= 0.0001f)
			{
				return SilentDecibels;
			}

			return Math.Max(SilentDecibels, (float)(20 * Math.Log10(Math.Min(1f, linear))));
		}
	}
}
