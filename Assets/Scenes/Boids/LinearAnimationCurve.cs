using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.Boids
{
	internal struct LinearAnimationCurve
	{
		private KeyValuePair<float, float> _key0;
		private KeyValuePair<float, float> _key1;

		public LinearAnimationCurve(KeyValuePair<float, float> key0, KeyValuePair<float, float> key1)
		{
			if (_key0.Key < _key1.Key)
			{
				_key0 = key0;
				_key1 = key1;
			}
			else
			{
				_key0 = key1;
				_key1 = key0;
			}
		}

		public float Evaluate(float time)
		{
			float time01 = math.unlerp(_key0.Key, _key1.Key, time);
			float value = math.lerp(_key0.Value, _key1.Value, time01);
			return math.clamp(value, _key0.Value, _key1.Value);
		}

		public static explicit operator LinearAnimationCurve(AnimationCurve animationCurve)
		{
			Keyframe keyFrame0 = animationCurve.keys[0];
			Keyframe keyFrame1 = animationCurve.keys[1];
			return new LinearAnimationCurve(
				new KeyValuePair<float, float>(keyFrame0.time, keyFrame0.value),
				new KeyValuePair<float, float>(keyFrame1.time, keyFrame1.value));
		}
	}
}