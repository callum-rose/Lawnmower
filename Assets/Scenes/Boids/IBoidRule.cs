using System.Collections.Generic;
using UnityEngine;

internal interface IBoidRule
{
	Vector3 CalcAcceleration(Boid boid, IList<Boid> otherBoids);
}