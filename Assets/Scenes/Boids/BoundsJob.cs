using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.Boids
{
	[BurstCompile]
	internal struct BoundsJob : IJobParallelFor
	{
		private Bounds _bounds;
	
		[ReadOnly]
		private NativeArray<Boid.Data> _allBoids;

		[WriteOnly]
		private NativeArray<Vector3> _resultAccelerations;

		[ReadOnly]
		private LinearAnimationCurve _effectByDistance;

		public BoundsJob(Bounds bounds, NativeArray<Boid.Data> allBoids, NativeArray<Vector3> resultAccelerations, LinearAnimationCurve effectByDistance)
		{
			_bounds = bounds;
			_allBoids = allBoids;
			_resultAccelerations = resultAccelerations;
			_effectByDistance = effectByDistance;
		}

		public void Execute(int index)
		{
			Boid.Data boid = _allBoids[index];
		
			if (_bounds.Contains(boid.Position))
			{
				return;
			}

			Vector3 insideBy = Vector3.zero;
			if (boid.Position.x < _bounds.min.x)
			{
				insideBy.x = _bounds.min.x - boid.Position.x;
			}
			else if (boid.Position.x > _bounds.max.x)
			{
				insideBy.x = _bounds.max.x - boid.Position.x;
			}

			if (boid.Position.y < _bounds.min.y)
			{
				insideBy.y = _bounds.min.y - boid.Position.y;
			}
			else if (boid.Position.y > _bounds.max.y)
			{
				insideBy.y = _bounds.max.y - boid.Position.y;
			}

			if (boid.Position.z < _bounds.min.z)
			{
				insideBy.z = _bounds.min.z - boid.Position.z;
			}
			else if (boid.Position.z > _bounds.max.z)
			{
				insideBy.z = _bounds.max.z - boid.Position.z;
			}

			float maxOutsideAxis = math.max(insideBy.x, math.max(insideBy.y, insideBy.z));
			float effect = _effectByDistance.Evaluate(maxOutsideAxis);
			Vector3 acceleration = insideBy * effect;
			_resultAccelerations[index] = acceleration;
		}
	}
}