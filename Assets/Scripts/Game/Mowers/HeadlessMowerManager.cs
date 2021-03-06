using Core;
using Game.Levels;
using Game.UndoSystem;
using UnityEngine;

namespace Game.Mowers
{
	[CreateAssetMenu(fileName = nameof(HeadlessMowerManager), menuName = SONames.GameDir + nameof(HeadlessMowerManager))]
	internal class HeadlessMowerManager : ScriptableObject, IMowerManager
	{
		[SerializeField] private MowerMovementManager mowerMovementManager;
		
		public void Construct(MowerMovementManager mowerMovementManager)
		{
			this.mowerMovementManager = mowerMovementManager;
		}

		public IMowerPosition Init(ILevelTraversalChecker levelTraversalChecker, IUndoSystem undoManager)
		{
			MowerMover mowerMover = new MowerMover();
			InitMowerMovementManager(mowerMover, levelTraversalChecker, undoManager);

			return mowerMover;
		}

		public void InitMowerMovementManager(MowerMover newMower, ILevelTraversalChecker levelTraversalChecker,
			IUndoSystem undoManager)
		{
			mowerMovementManager.Init(newMower, levelTraversalChecker, undoManager);
		}

		public void Clear()
		{
			mowerMovementManager.Clear();
		}
	}
}