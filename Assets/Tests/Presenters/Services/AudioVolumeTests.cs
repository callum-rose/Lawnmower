using NUnit.Framework;

namespace Lawnmower.Presenters.Tests
{
	public class AudioVolumeTests
	{
		[TestCase(1f, 0f)]
		[TestCase(0.5f, -6.0206f)]
		[TestCase(0.1f, -20f)]
		[TestCase(0f, -80f)]
		[TestCase(-1f, -80f)]
		[TestCase(3f, 0f)]
		[TestCase(float.NaN, -80f)]
		public void ToDecibels(float linear, float expected)
		{
			Assert.AreEqual(expected, AudioVolume.ToDecibels(linear), 1e-3);
		}

		[Test]
		public void ToDecibels_NeverGoesBelowTheFloor()
		{
			Assert.AreEqual(AudioVolume.SilentDecibels, AudioVolume.ToDecibels(0.00009f), 1e-3);
		}
	}
}
