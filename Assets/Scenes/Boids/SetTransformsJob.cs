using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace Scenes.Boids
{
	[BurstCompile]
	internal struct SetTransformsJob : IJobParallelForTransform
	{
		private NativeArray<Boid.Data> _boidData;

		[ReadOnly] private readonly NativeArray<Vector3> _avoidAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _alignAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _cohesionAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _boundsAccelerations;
		
		[ReadOnly] private readonly float _deltaTime;

		public SetTransformsJob(
			NativeArray<Boid.Data> boidData,
			NativeArray<Vector3> avoidAccelerations,
			NativeArray<Vector3> alignAccelerations,
			NativeArray<Vector3> cohesionAccelerations,
			NativeArray<Vector3> boundsAccelerations,
			float deltaTime)
		{
			_boidData = boidData;
			_avoidAccelerations = avoidAccelerations;
			_alignAccelerations = alignAccelerations;
			_cohesionAccelerations = cohesionAccelerations;
			_boundsAccelerations = boundsAccelerations;
			_deltaTime = deltaTime;
		}

		public void Execute(int index, TransformAccess transform)
		{
			Boid.Data boid = _boidData[index];
			
			boid.Position += boid.Velocity * _deltaTime;
			
			transform.position = boid.Position;
			transform.rotation = Quaternion.LookRotation(boid.Velocity, Vector3.up);

			_boidData[index] = boid;
		}
	}
}