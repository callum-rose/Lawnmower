using System.Collections.Generic;
using UnityEngine;

internal class AlignRule : IBoidRule
{
	private AnimationCurve _effectByDistance;

	public AlignRule(AnimationCurve effectByDistance)
	{
		Update(effectByDistance);
	}

	public void Update(AnimationCurve effectByDistance)
	{
		_effectByDistance = effectByDistance;
	}

	public Vector3 CalcAcceleration(Boid boid, IList<Boid> otherBoids)
	{
		Vector3 acceleration = Vector3.zero;
		foreach (Boid otherBoid in otherBoids)
		{
			float distance = boid.DistanceFrom(otherBoid);
			float effect = _effectByDistance.Evaluate(distance);
			acceleration += Vector3.Lerp(boid.Velocity, otherBoid.Velocity, effect);
		}

		return acceleration;
	}
}