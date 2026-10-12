using Game.Mowers.Input;
using System.Linq;
using Core.EventChannels;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Mowers
{
	internal class MowerManager : MonoBehaviour
	{
		[Header("Scene")]
		[SerializeField] private MowerObjectCreator mowerObjectCreator;
		[SerializeField] private INeedMowerPositionContainer[] mowerPositionRequirers;

		[Header("Event Channels")]
		[SerializeField] private IGameObjectEventChannelTransmitterContainer mowerCreatedEventChannelContainer;
		[SerializeField] private IGameObjectEventChannelTransmitterContainer mowerWillBeDestroyedEventChannelContainer;

		private IGameObjectEventChannelTransmitter MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;
		private IGameObjectEventChannelTransmitter MowerWillBeDestroyedEventChannel => mowerWillBeDestroyedEventChannelContainer.Result;
		
		private MowerObject _mowerObject;

		public void Init(MowerData mower, IMowerPosition mowerPosition)
		{
			_mowerObject = mowerObjectCreator.Create(mower, mowerPosition);

			InitObjectsNeedingMowerPosition(mowerPosition);
			InitCollider(_mowerObject.gameObject);

			MowerCreatedEventChannel.Raise(_mowerObject.gameObject);
		}

		public void Clear()
		{
			if (_mowerObject == null)
			{
				return;
			}

			MowerWillBeDestroyedEventChannel.Raise(_mowerObject.gameObject);

			_mowerObject.Dispose();
			Destroy(_mowerObject.gameObject);
		}

		private static void InitCollider(GameObject gameObject)
		{
			SphereCollider collider = gameObject.AddComponent<SphereCollider>();
			collider.radius = TileSize * 0.5f;
			collider.center = new Vector3(0, TileSize * 0.5f, 0);
		}

		private void InitObjectsNeedingMowerPosition(IMowerPosition position)
		{
			foreach (INeedMowerPosition mpr in mowerPositionRequirers.Select(c => c.Result))
			{
				mpr.Set(position);
			}
		}
	}
}