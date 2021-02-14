using System.Collections.Generic;
using UnityEngine;

namespace Scenes.Boids
{
	internal interface IBoidRule
	{
		Vector3 CalcAcceleration(Boid boid, IList<Boid> otherBoids);
	}
}