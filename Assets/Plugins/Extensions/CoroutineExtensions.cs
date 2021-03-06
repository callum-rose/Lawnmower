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
	}
}