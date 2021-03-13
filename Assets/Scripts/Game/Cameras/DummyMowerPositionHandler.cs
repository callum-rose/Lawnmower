using Core.EventChannels;
using UnityEngine;

namespace Game.Cameras
{
	internal class DummyMowerPositionHandler : MonoBehaviour
	{
		[SerializeField] private Transform dummyMower;
		[SerializeField] private IVector3EventChannelListenerContainer mowerObjectMovedEventChannel;
		private IVector3EventChannelListener MowerObjectMovedEventChannel => mowerObjectMovedEventChannel.Result;

		private void OnEnable()
		{
			MowerObjectMovedEventChannel.EventRaised += OnMowerObjectMoved;
		}

		private void OnDisable()
		{
			MowerObjectMovedEventChannel.EventRaised -= OnMowerObjectMoved;
		}

		private void OnMowerObjectMoved(Vector3 position)
		{
			dummyMower.transform.position = position;
		}
	}
}