using System.Collections.Generic;
using UnityEngine;

internal class AvoidRule : IBoidRule
{
	private AnimationCurve _effectByDistance;

	public AvoidRule(AnimationCurve effectByDistance)
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
			Vector3 displacement = boid.DisplacementFrom(otherBoid);
			float distance = boid.DistanceFrom(otherBoid);
			float effect = _effectByDistance.Evaluate(distance);

			acceleration += effect * displacement;
		}

		return acceleration;
	}
}