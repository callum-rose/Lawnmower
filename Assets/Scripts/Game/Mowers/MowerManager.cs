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
		[TitleGroup("Scene")] [SerializeField] private MowerObjectCreator mowerObjectCreator;
		[SerializeField] private IMowerControlsContainer[] mowerControls;
		[SerializeField] private IRequiresMowerPositionContainer[] mowerPositionRequirers;
		[SerializeField] private HeadlessMowerManager headlessMowerManager;

		[TitleGroup("Event Channels")] 
		[SerializeField] private GameObjectEventChannel mowerCreatedEventChannel;
		[SerializeField] private GameObjectEventChannel mowerWillBeDestroyedEventChannel;

		private MowerObject _mowerObject;

		public void Init(MowerData mower, ILevelTraversalChecker levelTraversalChecker, IUndoSystem undoManager)
		{
			MowerMover mowerMover = new MowerMover();

			_mowerObject = mowerObjectCreator.Create(mower, mowerMover);

			IMowerControls[] controls = mowerControls.Select(c => c.Result).ToArray();
			headlessMowerManager.InitMowerMovementManager(mowerMover, levelTraversalChecker, undoManager, controls);
			
			InitObjectsNeedingMowerPosition(mowerMover);
			InitCollider(_mowerObject.gameObject);

			mowerCreatedEventChannel.Raise(_mowerObject.gameObject);
		}

		public void Clear()
		{
			headlessMowerManager.Clear();
			
			if (_mowerObject == null)
			{
				return;
			}

			mowerWillBeDestroyedEventChannel.Raise(_mowerObject.gameObject);

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
			foreach (IRequiresMowerPosition mpr in mowerPositionRequirers.Select(c => c.Result))
			{
				mpr.Init(position);
			}
		}
	}
}