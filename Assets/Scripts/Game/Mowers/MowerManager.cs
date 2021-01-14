using Game.Mowers.Input;
using Game.UndoSystem;
using System.Linq;
using Core;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Mowers
{
	internal class MowerManager : MonoBehaviour
	{
		[SerializeField] private MowerObjectCreator mowerObjectCreator;
		[SerializeField] private MowerMovementManager mowerMovementManager;
		[SerializeField] private IMowerControlsContainer[] mowerControls;
		[SerializeField] private IRequiresMowerPositionContainer[] mowerPositionRequirers;

		public GameObject Create(MowerData mower, IUndoSystem undoManager)
		{
			MowerMoverr mowerMoverr = new MowerMoverr();

			GameObject mowerObject = mowerObjectCreator.Create(mower, mowerMoverr);

			InitMowerMovement(mowerMoverr, undoManager);
			InitObjectsNeedingMowerPosition(mowerMoverr);
			InitCollider(mowerObject);

			return mowerObject;
		}

		private static void InitCollider(GameObject gameObject)
		{
			SphereCollider collider = gameObject.AddComponent<SphereCollider>();
			collider.radius = TileSize * 0.5f;
			collider.center = new Vector3(0, TileSize * 0.5f, 0);
		}

		private void InitMowerMovement(MowerMoverr newMower, IUndoSystem undoManager)
		{
			IMowerControls[] controls = mowerControls.Select(c => c.Result).ToArray();
			mowerMovementManager.Init(newMower, controls, undoManager);
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