using UnityEngine;

internal interface IVelocityPostProcessor
{
	Vector3 Process(Vector3 input);
}