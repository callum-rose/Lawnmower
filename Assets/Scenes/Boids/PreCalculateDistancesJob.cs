using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Scenes.Boids
{
	internal struct PreCalculateDistancesJob : IJobParallelFor
	{
		[ReadOnly] private NativeArray<Boid.Data> _boidData;

		[WriteOnly] private NativeArray<float> _resultDistances;

		public PreCalculateDistancesJob(NativeArray<Boid.Data> boidData, NativeArray<float> resultDistances)
		{
			_boidData = boidData;
			_resultDistances = resultDistances;
		}

		public void Execute(int index)
		{
			Boid.Data boid = _boidData[index];
			for (int i = 0; i < _boidData.Length; i++)
			{
				if (i == index)
				{
					continue;
				}

				Boid.Data otherBoid = _boidData[i];
				
				float distance = math.distance(boid.Position, otherBoid.Position);
				
				_resultDistances[index * _boidData.Length + i] = distance;
			}
		}
	}
}