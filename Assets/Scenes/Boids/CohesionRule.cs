using System.Collections.Generic;
using UnityEngine;

internal class CohesionRule : IBoidRule
{
	private AnimationCurve _effectByDistance;

	public CohesionRule(AnimationCurve effectByDistance)
	{
		Update(effectByDistance);
	}

	public void Update(AnimationCurve effectByDistance)
	{
		_effectByDistance = effectByDistance;
	}

	public Vector3 CalcAcceleration(Boid boid, IList<Boid> otherBoids)
	{
		Vector3 weightedSumPosition = Vector3.zero;
		float sumWeight = 0;
		foreach (Boid otherBoid in otherBoids)
		{
			float distance = boid.DistanceFrom(otherBoid);
			float weight = _effectByDistance.Evaluate(distance);

			weightedSumPosition += otherBoid.Position * weight;
			sumWeight += weight;
		}

		if (sumWeight == 0)
		{
			return Vector3.zero;
		}

		Vector3 weightedCenter = weightedSumPosition / sumWeight;
		float distanceToCenter = Vector3.Distance(boid.Position, weightedCenter);

		float effect = _effectByDistance.Evaluate(distanceToCenter);
		return (weightedCenter - boid.Position) * effect;
	}
}