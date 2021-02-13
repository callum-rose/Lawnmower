using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

internal struct SetTransformsJob : IJobParallelForTransform
{
	[Unity.Collections.ReadOnly]
	public NativeArray<Boid.Data> BoidData;
	
	public void Execute(int index, TransformAccess transform)
	{
		Boid.Data boid = BoidData[index];
		transform.position = boid.Position;
		transform.rotation = Quaternion.LookRotation(boid.Velocity, Vector3.up);
	}
}