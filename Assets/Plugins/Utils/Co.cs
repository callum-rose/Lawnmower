using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
	public static AnimationCurve EaseIn(this AnimationCurve a)
	{
		return new AnimationCurve(new Keyframe(0, 0, 0, 0), new Keyframe(1, 1, 2, 0));
	}

	public static AnimationCurve EaseOutElastic(this AnimationCurve a)
	{
		return new AnimationCurve(
			new Keyframe(0, 0, 0, 3.5f),
			new Keyframe(0.63f, 1.1f, 0, 0),
			new Keyframe(0.88f, 0.95f, 0, 0),
			new Keyframe(1, 1, 0, 0)
		);
	}

	public static AnimationCurve EaseOut(this AnimationCurve a)
	{
		return new AnimationCurve(new Keyframe(0, 0, 0, 2), new Keyframe(1, 1, 0, 0));
	}
}

public class Co : MonoBehaviour
{
	private static Co _instance;

	public static Co instance
	{
		get
		{
			if (!_instance)
			{
				_instance = new GameObject("Co Daemon").AddComponent<Co>();
				DontDestroyOnLoad(_instance.gameObject);
			}

			return _instance;
		}
	}

	public enum Ease
	{
		Linear,
		In,
		Out,
		InOut,
		OutElastic
	}


	static AnimationCurve GetCurve(Ease ease)
	{
		if (ease == Ease.Linear) return AnimationCurve.Linear(0, 0, 1, 1);
		if (ease == Ease.In) return new AnimationCurve().EaseIn();
		if (ease == Ease.Out) return new AnimationCurve().EaseOut();
		if (ease == Ease.InOut) return AnimationCurve.EaseInOut(0, 0, 1, 1);
		if (ease == Ease.OutElastic) return new AnimationCurve().EaseOutElastic();
		return null;
	}

	public class Routine
	{
		Queue<Action> actionQueue = new Queue<Action>();
		private bool unscaled;

		public Routine Start()
		{
			actionQueue.Dequeue().Invoke();
			return this;
		}

		public Routine Unscaled()
		{
			unscaled = true;
			return this;
		}

		public Routine Wait(float time)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_Wait(time)));
			return this;
		}

		IEnumerator _Wait(float delay)
		{
			if (unscaled) yield return new WaitForSecondsRealtime(delay);
			else yield return new WaitForSeconds(delay);
			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}

		public Routine WaitForFrames(int frameCount)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_WaitForFrames(frameCount)));
			return this;
		}

		IEnumerator _WaitForFrames(int frameCount)
		{
			for (int i = 0; i < frameCount; i++) yield return null;
			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}

		public Routine Animate(float duration, Action<float> action, Ease ease = Ease.Linear)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_Animate(duration, action, GetCurve(ease))));
			return this;
		}

		public Routine Animate(float duration, Action<float> action, AnimationCurve animationCurve)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_Animate(duration, action, animationCurve)));
			return this;
		}

		IEnumerator _Animate(float duration, Action<float> action, AnimationCurve animationCurve)
		{
			var i = 0f;
			while (i < 1)
			{
				i += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) * 1f / duration;
				if (i > 1) i = 1;
				action.Invoke(animationCurve.Evaluate(i));
				yield return null;
			}

			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}

		public Routine WaitUntil(Func<bool> condition)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_WaitUntil(condition)));
			return this;
		}

		IEnumerator _WaitUntil(Func<bool> condition)
		{
			while (!condition.Invoke()) yield return null;
			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}

		public Routine DoUntil(Func<bool> condition, Action action)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_DoUntil(condition, action)));
			return this;
		}

		IEnumerator _DoUntil(Func<bool> condition, Action action)
		{
			while (!condition.Invoke())
			{
				action.Invoke();
				yield return null;
			}

			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}

		public Routine DoForDuration(float duration, Action action)
		{
			actionQueue.Enqueue(() => instance.StartCoroutine(_DoForDuration(duration, action)));
			return this;
		}

		IEnumerator _DoForDuration(float duration, Action action)
		{
			var i = 0f;
			while (i < duration)
			{
				action.Invoke();
				i += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
				yield return null;
			}

			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}

		public Routine Then(Action action)
		{
			actionQueue.Enqueue(() => _Then(action));
			return this;
		}

		void _Then(Action action)
		{
			action.Invoke();
			if (actionQueue.Count > 0) actionQueue.Dequeue().Invoke();
		}
	}

	public static Routine Wait(float time)
	{
		return new Routine().Wait(time).Start();
	}

	public static Routine WaitForFrames(int frameCount)
	{
		return new Routine().WaitForFrames(frameCount).Start();
	}

	public static Routine WaitOneFrame()
	{
		return new Routine().WaitForFrames(1).Start();
	}

	public static Routine WaitUntil(Func<bool> condition)
	{
		return new Routine().WaitUntil(condition).Start();
	}

	public static Routine DoUntil(Func<bool> condition, Action action)
	{
		return new Routine().DoUntil(condition, action).Start();
	}

	public static Routine DoForDuration(float duration, Action action)
	{
		return new Routine().DoForDuration(duration, action).Start();
	}

	public static Routine Animate(float duration, Action<float> action)
	{
		return new Routine().Animate(duration, action).Start();
	}
}