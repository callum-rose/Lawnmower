using UnityEngine;

namespace Scenes.Boids
{
	internal interface IVelocityPostProcessor
	{
		Vector3 Process(Vector3 input);
	}
}