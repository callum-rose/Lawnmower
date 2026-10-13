using UnityEngine;

namespace Game.Levels
{
	/// <summary>Swims a creature round a circle centred where it was placed, facing the way it's going (+Z forward).</summary>
	internal class PondSwimmer : MonoBehaviour
	{
		private Vector3 _center;
		private float _radius;
		private float _angularSpeed;
		private float _angle;

		public void Init(float radius, float speed, float startTurn)
		{
			_center = transform.position;
			_radius = radius;
			_angularSpeed = speed / radius;
			_angle = startTurn * Mathf.PI * 2;
			Apply();
		}

		private void Update()
		{
			_angle += _angularSpeed * Time.deltaTime;
			Apply();
		}

		private void Apply()
		{
			Vector3 offset = new Vector3(Mathf.Cos(_angle), 0, Mathf.Sin(_angle));
			Vector3 direction = new Vector3(-offset.z, 0, offset.x);
			transform.SetPositionAndRotation(_center + offset * _radius, Quaternion.LookRotation(direction));
		}
	}
}
