using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Mowers.Models
{
	[ExecuteInEditMode]
	public class SpringBallJoint : MonoBehaviour
	{
		[SerializeField] private Transform sourceTransform;
		[SerializeField, Range(0, 1)] private float spring;
		[SerializeField, Range(0, 1)] private float damping;
		[SerializeField] private Quaternion targetRotation;

		private Vector3 _lastSourcePosition;

		private void Start()
		{
			SetLastPositions();
		}

		private void Update()
		{
			Vector3 sourceVelocity = (sourceTransform.position - _lastSourcePosition) / Time.deltaTime;

			transform.rotation = Quaternion.SlerpUnclamped(Quaternion.LookRotation(-sourceVelocity.normalized, Vector3.up),
				transform.rotation, damping);
			transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, targetRotation, spring);
		}

		private void LateUpdate()
		{
			SetLastPositions();
		}

		private void SetLastPositions()
		{
			_lastSourcePosition = sourceTransform.position;
		}

		[Button]
		private void ApplyTargetAngle()
		{
			targetRotation = transform.localRotation;
		}
	}
}