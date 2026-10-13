using System;
using System.Collections;
using UnityEngine;

namespace BunnyHeaven.UI
{
    /// <summary>Tiny unscaled-time tween helper so UI keeps animating while the game is paused.</summary>
    public static class UiTween
    {
        public static Coroutine Run(MonoBehaviour host, float duration, Action<float> step, Action done = null)
        {
            if (!host.isActiveAndEnabled || !Application.isPlaying)
            {
                step(1f);
                done?.Invoke();
                return null;
            }

            return host.StartCoroutine(Routine(duration, step, done));
        }

        private static IEnumerator Routine(float duration, Action<float> step, Action done)
        {
            var t = 0f;
            while (t < duration)
            {
                step(Mathf.Clamp01(t / duration));
                yield return null;
                t += Time.unscaledDeltaTime;
            }

            step(1f);
            done?.Invoke();
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    }
}
