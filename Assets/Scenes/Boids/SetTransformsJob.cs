using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
internal struct SetTransformsJob : IJobParallelForTransform
{
	private NativeArray<Boid.Data> _boidData;

	[ReadOnly] private readonly NativeArray<Vector3> _avoidAccelerations;
	[ReadOnly] private readonly NativeArray<Vector3> _alignAccelerations;
	[ReadOnly] private readonly NativeArray<Vector3> _cohesionAccelerations;
	[ReadOnly] private readonly NativeArray<Vector3> _boundsAccelerations;

	[ReadOnly] private readonly Vector2 _speedRange;

	[ReadOnly] private readonly float _deltaTime;

	public SetTransformsJob(
		NativeArray<Boid.Data> boidData,
		NativeArray<Vector3> avoidAccelerations,
		NativeArray<Vector3> alignAccelerations,
		NativeArray<Vector3> cohesionAccelerations,
		NativeArray<Vector3> boundsAccelerations,
		Vector2 speedRange,
		float deltaTime)
	{
		_boidData = boidData;
		_avoidAccelerations = avoidAccelerations;
		_alignAccelerations = alignAccelerations;
		_cohesionAccelerations = cohesionAccelerations;
		_boundsAccelerations = boundsAccelerations;
		_speedRange = speedRange;
		_deltaTime = deltaTime;
	}

	public void Execute(int index, TransformAccess transform)
	{
		Boid.Data boid = _boidData[index];

		Vector3 totalAcceleration = _avoidAccelerations[index] + _alignAccelerations[index] +
		                            _cohesionAccelerations[index] + _boundsAccelerations[index];

		boid.Velocity += totalAcceleration * _deltaTime;

		float speed = boid.Velocity.magnitude;
		if (speed > _speedRange.y)
		{
			boid.Velocity = boid.Velocity.normalized * _speedRange.y;
		}
		else if (speed < _speedRange.x)
		{
			boid.Velocity = boid.Velocity.normalized * _speedRange.x;
		}

		boid.Velocity *= 0.99f;

		boid.Position += boid.Velocity;

		transform.position = boid.Position;
		transform.rotation = Quaternion.LookRotation(boid.Velocity, Vector3.up);

		_boidData[index] = boid;
	}
}