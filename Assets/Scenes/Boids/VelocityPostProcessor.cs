using UnityEngine;

namespace Scenes.Boids
{
	internal struct VelocityPostProcessor : IVelocityPostProcessor
	{
		private readonly float _min;
		private readonly float _max;

		public VelocityPostProcessor(float min, float max)
		{
			_min = min;
			_max = max;
		}

		public Vector3 Process(Vector3 input)
		{
			float speed = input.magnitude;
			if (speed < _min)
			{
				return input.normalized * _min;
			}

			if (speed > _max)
			{
				return input.normalized * _max;
			}

			return input;
		}
	}
	
	internal struct Velocity2DPostProcessor : IVelocityPostProcessor
	{
		private VelocityPostProcessor _velocityPostProcessor;

		public Velocity2DPostProcessor(float min, float max)
		{
			_velocityPostProcessor = new VelocityPostProcessor(min, max);
		}

		public Vector3 Process(Vector3 input)
		{
			input.y = 0;
			return _velocityPostProcessor.Process(input);
		}
	}
}