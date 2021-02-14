using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.Boids
{
	[BurstCompile]
	internal struct CohesionJob : IJobParallelFor
	{
		[ReadOnly]
		private NativeArray<Boid.Data> _allBoids;

		[WriteOnly]
		private NativeArray<Vector3> _resultAccelerations;

		[ReadOnly]
		private LinearAnimationCurve _effectByDistance;

		public CohesionJob(NativeArray<Boid.Data> allBoids, NativeArray<Vector3> resultAccelerations, LinearAnimationCurve effectByDistance)
		{
			_allBoids = allBoids;
			_resultAccelerations = resultAccelerations;
			_effectByDistance = effectByDistance;
		}

		public void Execute(int index)
		{
			Boid.Data boid = _allBoids[index];

			Vector3 weightedSumPosition = Vector3.zero;
			float sumWeight = 0;
			for (int i = 0; i < _allBoids.Length; i++)
			{
				if (i == index)
				{
					// skip self
					continue;
				}

				Boid.Data otherBoid = _allBoids[i];

				float distanceSqr = math.distancesq(boid.Position, otherBoid.Position);

				if (distanceSqr > 2 * 2)
				{
					continue;
				}
				
				float distance = math.sqrt(distanceSqr);
				float weight = _effectByDistance.Evaluate(distance);

				weightedSumPosition += otherBoid.Position * weight;
				sumWeight += weight;
			}

			if (sumWeight == 0)
			{
				_resultAccelerations[index] = Vector3.zero;
				return;
			}

			Vector3 weightedCenter = weightedSumPosition / sumWeight;
			float distanceToCenter = Vector3.Distance(boid.Position, weightedCenter);

			float effect = _effectByDistance.Evaluate(distanceToCenter);
			_resultAccelerations[index] = (weightedCenter - boid.Position) * effect;
		}
	}
}