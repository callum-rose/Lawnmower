using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.Boids
{
	[BurstCompile]
	internal struct AvoidJob : IJobParallelFor
	{
		[ReadOnly]
		private NativeArray<Boid.Data> _allBoids;

		[WriteOnly]
		private NativeArray<Vector3> _resultAccelerations;

		[ReadOnly]
		private LinearAnimationCurve _effectByDistance;

		public AvoidJob(NativeArray<Boid.Data> allBoids, NativeArray<Vector3> resultAccelerations, LinearAnimationCurve effectByDistance)
		{
			_allBoids = allBoids;
			_resultAccelerations = resultAccelerations;
			_effectByDistance = effectByDistance;
		}

		public void Execute(int index)
		{
			Vector3 acceleration = Vector3.zero;

			Boid.Data boid = _allBoids[index];
		
			for (int i = 0; i < _allBoids.Length; i++)
			{
				if (i == index)
				{
					// skip self
					continue;
				}

				Boid.Data otherBoid = _allBoids[i];

				Vector3 displacement = boid.DisplacementFrom(otherBoid);
				float distanceSqr = math.distancesq(boid.Position, otherBoid.Position);

				if (distanceSqr > 2 * 2)
				{
					continue;
				}
				
				float distance = math.sqrt(distanceSqr);
				float effect = _effectByDistance.Evaluate(distance);

				acceleration += effect * displacement;
			}

			_resultAccelerations[index] = acceleration;
		}
	}
}