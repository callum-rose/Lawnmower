using UnityEngine;

namespace Core
{
	/// <summary>
	/// iOS and Android cap the frame rate at 30 unless targetFrameRate is set, so set it before the first scene loads.
	/// </summary>
	public static class FrameRateSetup
	{
		public const int TargetFrameRate = 60;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Apply()
		{
			Application.targetFrameRate = TargetFrameRate;
		}
	}
}
