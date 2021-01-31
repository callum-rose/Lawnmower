using Core;
using Game.Levels;
using Game.Mowers.Input;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(HeadlessMowerManager), menuName = SONames.GameDir + nameof(HeadlessMowerManager))]
	internal class HeadlessMowerManager : ScriptableObject, IMowerManager
	{
		[SerializeField] private MowerMovementManager mowerMovementManager;
		
		private IMowerControls[] _mowerControls;

		public void Construct(MowerMovementManager mowerMovementManager, IMowerControls[] mowerControls)
		{
			this.mowerMovementManager = mowerMovementManager;
			_mowerControls = mowerControls;
		}

		public IMowerPosition Init(ILevelTraversalChecker levelTraversalChecker, IUndoSystem undoManager)
		{
			MowerMover mowerMover = new MowerMover();
			InitMowerMovementManager(mowerMover, levelTraversalChecker, undoManager, _mowerControls);

			return mowerMover;
		}

		public void InitMowerMovementManager(MowerMover newMower, ILevelTraversalChecker levelTraversalChecker,
			IUndoSystem undoManager, IMowerControls[] controls)
		{
			mowerMovementManager.Init(newMower, controls, levelTraversalChecker, undoManager);
		}

		public void Clear()
		{
			mowerMovementManager.Clear();
		}
	}
}