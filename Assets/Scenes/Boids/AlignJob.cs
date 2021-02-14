using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.Boids
{
	[BurstCompile]
	internal struct AlignJob : IJobParallelFor
	{
		[ReadOnly]
		private NativeArray<Boid.Data> _allBoids;

		[ReadOnly]
		private readonly NativeArray<float> _boidDistances;

		[WriteOnly]
		private NativeArray<Vector3> _resultAccelerations;

		[ReadOnly]
		private LinearAnimationCurve _effectByDistance;

		public AlignJob(
			NativeArray<Boid.Data> allBoids,
			NativeArray<Vector3> resultAccelerations,
			LinearAnimationCurve effectByDistance)
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

				float distanceSqr = math.distancesq(boid.Position, otherBoid.Position);

				if (distanceSqr > 2 * 2)
				{
					continue;
				}

				float distance = math.sqrt(distanceSqr);
				float effect = _effectByDistance.Evaluate(distance);
				acceleration += Vector3.Lerp(boid.Velocity, otherBoid.Velocity, effect);
			}

			_resultAccelerations[index] = acceleration;
		}
	}
}