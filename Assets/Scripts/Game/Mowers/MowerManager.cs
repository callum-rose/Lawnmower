using Game.Mowers.Input;
using Game.UndoSystem;
using System.Linq;
using Core.EventChannels;
using Game.Levels;
using Sirenix.OdinInspector;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Mowers
{
	internal class MowerManager : MonoBehaviour, IMowerManager
	{
		[TitleGroup("Scene")]
		[SerializeField] private MowerObjectCreator mowerObjectCreator;
		[SerializeField] private INeedMowerPositionContainer[] mowerPositionRequirers;
		[SerializeField] private HeadlessMowerManager headlessMowerManager;

		[TitleGroup("Event Channels")]
		[SerializeField] private IGameObjectEventChannelTransmitterContainer mowerCreatedEventChannelContainer;
		[SerializeField] private IGameObjectEventChannelTransmitterContainer mowerWillBeDestroyedEventChannelContainer;

		private IGameObjectEventChannelTransmitter MowerCreatedEventChannel => mowerCreatedEventChannelContainer.Result;
		private IGameObjectEventChannelTransmitter MowerWillBeDestroyedEventChannel => mowerWillBeDestroyedEventChannelContainer.Result;
		
		private MowerObject _mowerObject;

		public void Init(MowerData mower, ILevelTraversalChecker levelTraversalChecker, IUndoSystem undoManager)
		{
			MowerMover mowerMover = new MowerMover();

			_mowerObject = mowerObjectCreator.Create(mower, mowerMover);

			headlessMowerManager.InitMowerMovementManager(mowerMover, levelTraversalChecker, undoManager);

			InitObjectsNeedingMowerPosition(mowerMover);
			InitCollider(_mowerObject.gameObject);

			MowerCreatedEventChannel.Raise(_mowerObject.gameObject);
		}

		public void Clear()
		{
			headlessMowerManager.Clear();

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