using Game.Core;
using Game.Mowers.Input;
using Game.UndoSystem;
using System.Linq;
using UnityEngine;
using static Game.Levels.LevelDimensions;

namespace Game.Mowers
{
    internal class MowerCreator : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private Positioner positioner;
        [SerializeField] private IMowerControlsContainer[] mowerControls;
        [SerializeField] private IRequiresMowerPositionContainer[] mowerPositionRequirers;

        public MowerManager Create(MowerData mower, IUndoSystem undoManager)
        {
            MowerManager newMower = Instantiate(mower.Prefab, container);

            InitMowerMovement(undoManager, newMower.Movement);
            InitObjectsNeedingMowerPosition(newMower.Movement);
            InitCollider(newMower.gameObject);
            
            return newMower;
        }

        private static void InitCollider(GameObject gameObject)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = TileSize * 0.5f;
            collider.center = new Vector3(0, TileSize * 0.5f, 0);
        }

        private void InitMowerMovement(IUndoSystem undoManager, MowerMovementManager movement)
        {
            IMowerControls[] controls = mowerControls.Select(c => c.Result).ToArray();
            movement.Init(
                controls,
                positioner,
                undoManager);
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
