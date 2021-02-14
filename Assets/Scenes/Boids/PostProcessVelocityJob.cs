using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Scenes.Boids
{
	internal struct PostProcessVelocity2DJob : IJobParallelFor
	{
		[ReadOnly] private readonly Vector2 _speedRange;
		private readonly float _deltaTime;
		private readonly float _drag;

		[ReadOnly] private readonly NativeArray<Vector3> _avoidAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _alignAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _cohesionAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _boundsAccelerations;

		private NativeArray<Boid.Data> _boidData;

		public PostProcessVelocity2DJob(
			Vector2 speedRange,
			float deltaTime,
			float drag,
			NativeArray<Vector3> avoidAccelerations,
			NativeArray<Vector3> alignAccelerations,
			NativeArray<Vector3> cohesionAccelerations,
			NativeArray<Vector3> boundsAccelerations,
			NativeArray<Boid.Data> boidData)
		{
			_speedRange = speedRange;
			_deltaTime = deltaTime;
			_drag = drag;
			_avoidAccelerations = avoidAccelerations;
			_alignAccelerations = alignAccelerations;
			_cohesionAccelerations = cohesionAccelerations;
			_boundsAccelerations = boundsAccelerations;
			_boidData = boidData;
		}

		public void Execute(int index)
		{
			Vector3 totalAcceleration = _avoidAccelerations[index] + _alignAccelerations[index] +
			                            _cohesionAccelerations[index] + _boundsAccelerations[index];
			totalAcceleration.y = 0;
		
			Boid.Data boid = _boidData[index];

			boid.Velocity += totalAcceleration;
			boid.Velocity -= boid.Velocity * _drag * _deltaTime;
			boid.Velocity.y = 0;

			float speed = boid.Velocity.magnitude;
			if (speed < _speedRange.x)
			{
				boid.Velocity *= _speedRange.x / speed;
			}
			else if (speed > _speedRange.y)
			{
				boid.Velocity *= _speedRange.y / speed;
			}

			_boidData[index] = boid;
		}
	}

	internal struct PostProcessVelocityJob : IJobParallelFor
	{
		[ReadOnly] private readonly Vector2 _speedRange;
		private readonly float _deltaTime;
		private readonly float _drag;

		[ReadOnly] private readonly NativeArray<Vector3> _avoidAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _alignAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _cohesionAccelerations;
		[ReadOnly] private readonly NativeArray<Vector3> _boundsAccelerations;

		private NativeArray<Boid.Data> _boidData;

		public PostProcessVelocityJob(
			Vector2 speedRange,
			float deltaTime,
			float drag,
			NativeArray<Vector3> avoidAccelerations,
			NativeArray<Vector3> alignAccelerations,
			NativeArray<Vector3> cohesionAccelerations,
			NativeArray<Vector3> boundsAccelerations,
			NativeArray<Boid.Data> boidData)
		{
			_speedRange = speedRange;
			_deltaTime = deltaTime;
			_drag = drag;
			_avoidAccelerations = avoidAccelerations;
			_alignAccelerations = alignAccelerations;
			_cohesionAccelerations = cohesionAccelerations;
			_boundsAccelerations = boundsAccelerations;
			_boidData = boidData;
		}

		public void Execute(int index)
		{
			Vector3 totalAcceleration = _avoidAccelerations[index] + _alignAccelerations[index] +
			                            _cohesionAccelerations[index] + _boundsAccelerations[index];

			Boid.Data boid = _boidData[index];

			boid.Velocity += totalAcceleration;
			boid.Velocity -= boid.Velocity * _drag * _deltaTime;

			float speed = math.length(boid.Velocity);
			if (speed < _speedRange.x)
			{
				boid.Velocity *= _speedRange.x / speed;
			}
			else if (speed > _speedRange.y)
			{
				boid.Velocity *= _speedRange.y / speed;
			}

			_boidData[index] = boid;
		}
	}
}