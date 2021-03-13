using System;
using System.Collections;
using UnityEngine;

namespace BalsamicBits.Extensions
{
	public static class CoroutineExtensions
	{
		public static IEnumerator Timer(float duration, Action callback = null)
		{
			float endTime = Time.time + duration;
			
			yield return new WaitUntil(() => Time.time >= endTime);
			
			callback?.Invoke();
		}

		public static Coroutine Timer(this MonoBehaviour monoBehaviour, float duration, Action callback = null)
		{
			return monoBehaviour.StartCoroutine(Timer(duration, callback));
		}

		public static IEnumerator WaitForFrames(int frames, Action callback = null)
		{
			for (int i = 0; i < frames; i++)
			{
				yield return null;
			}
			
			callback?.Invoke();
		}

		public static Coroutine WaitForFrames(this MonoBehaviour monoBehaviour, int frames, Action callback = null)
		{
			return monoBehaviour.StartCoroutine(WaitForFrames(frames, callback));
		}

		public static void Stop(this Coroutine coroutine, MonoBehaviour monoBehaviour)
		{
			if (coroutine != null)
			{
				monoBehaviour.StopCoroutine(coroutine);
			}
		}

		public static Coroutine Start(this IEnumerator enumerator, MonoBehaviour monoBehaviour)
		{
			return monoBehaviour.StartCoroutine(enumerator);
		}
	}
}