using UnityEngine;
using DG.Tweening;
using Game.Core;
using Game.UndoSystem;

namespace Game.Mowers
{
    internal class MowerMovementFacer : MonoBehaviour
    {
        [SerializeField] private MowerMovementManager movementController;
        [SerializeField] private float turnDuration = 0.25f;
        [SerializeField] private Ease turnAnimEase = Ease.InOutQuad;

        private void OnEnable()
        {
            movementController.Moved += MovementController_Moved;
        }

        private void OnDisable()
        {
            movementController.Moved -= MovementController_Moved;
        }

        private void MovementController_Moved(GridVector prevPosition, GridVector targetPosition, Xor isUndo)
        {
            GridVector directionToLookGrid = targetPosition - prevPosition;
            directionToLookGrid *= isUndo ? -1 : 1;

            Vector3 directionToLook = new Vector3(directionToLookGrid.x, 0, directionToLookGrid.y);

            transform.DOLookAt(transform.position + directionToLook, turnDuration).SetEase(turnAnimEase);
        }
    }
}
