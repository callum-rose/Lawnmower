using System;
using Core.EventChannels;
using UnityEngine;
using DG.Tweening;
using Game.Core;
using Game.Mowers.Models;
using Game.UndoSystem;
using Sirenix.OdinInspector;

namespace Game.Mowers
{
    internal class MowerPositionAnimator : MonoBehaviour
    {
        [SerializeField] private MowerMovementManager movementController;
        [SerializeField] private AnimationSpeedHandler animationSpeedHandler;

        [SerializeField] private float turnDuration = 0.25f;
        [SerializeField] private Ease turnAnimEase = Ease.InOutQuad;
        
        [TitleGroup("Event Channels")]
        [SerializeField] private Vector3EventChannel movedEventChannel;

        private MowerMover _mowerMover;
        private Positioner _positioner;

        private void OnEnable()
        {
            movementController.Moved += MovementController_Moved;
        }

        private void OnDisable()
        {
            movementController.Moved -= MovementController_Moved;
        }
        
        public void Init(MowerMover mowerMover, Positioner positioner)
        {
            _mowerMover = mowerMover;
            _positioner = positioner;
        }

        private void MovementController_Moved(GridVector prevPosition, GridVector targetPosition, Xor isUndo)
        {
            Look(prevPosition, targetPosition, isUndo);
        }

        private void Look(GridVector prevPosition, GridVector targetPosition, Xor isUndo)
        {
            GridVector directionToLookGrid = targetPosition - prevPosition;
            directionToLookGrid *= isUndo ? -1 : 1;

            Vector3 directionToLook = new Vector3(directionToLookGrid.x, 0, directionToLookGrid.y);

            transform.DORotateQuaternion(Quaternion.LookRotation(directionToLook, Vector3.up), turnDuration)
                .SetEase(turnAnimEase);
        }
        
        private void PositionChanged(GridVector position, Xor isInverted)
        {
            Vector3 worldPosition = _positioner.GetWorldPosition(position);

            _tween?.Kill();
			
            if (!isInverted)
            {
                _tween = transform
                    .DOMove(worldPosition, 0.1f).OnUpdate(() => movedEventChannel.Raise(transform.position))
                    .OnComplete(() => _tween = null);
            }
            else
            {
                transform.position = worldPosition;
                movedEventChannel.Raise(worldPosition);
            }
        }
    }
}
