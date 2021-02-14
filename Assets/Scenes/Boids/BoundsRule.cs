using System.Collections.Generic;
using UnityEngine;

namespace Scenes.Boids
{
	internal class BoundsRule : IBoidRule
	{
		private Bounds _bounds;
		private float _strength;

		public BoundsRule(Bounds bounds, float strength)
		{
			Update(bounds, strength);
		}

		public void Update(Bounds bounds, float strength)
		{
			_bounds = bounds;
			_strength = strength;
		}

		public Vector3 CalcAcceleration(Boid boid, IList<Boid> otherBoids)
		{
			if (_bounds.Contains(boid.Position))
			{
				return Vector3.zero;
			}

			Vector3 acceleration = Vector3.zero;
			if (boid.Position.x < _bounds.min.x)
			{
				acceleration.x += _bounds.min.x - boid.Position.x;
			}
			else if (boid.Position.x > _bounds.max.x)
			{
				acceleration.x += _bounds.max.x - boid.Position.x;
			}

			if (boid.Position.y < _bounds.min.y)
			{
				acceleration.y += _bounds.min.y - boid.Position.y;
			}
			else if (boid.Position.y > _bounds.max.y)
			{
				acceleration.y += _bounds.max.y - boid.Position.y;
			}

			if (boid.Position.z < _bounds.min.z)
			{
				acceleration.z += _bounds.min.z - boid.Position.z;
			}
			else if (boid.Position.z > _bounds.max.z)
			{
				acceleration.z += _bounds.max.z - boid.Position.z;
			}

			return acceleration * _strength;
		}
	}
}