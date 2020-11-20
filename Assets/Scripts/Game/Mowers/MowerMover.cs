using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;
using Game.Core;

namespace Game.Mowers
{
    internal class MowerMover : MonoBehaviour
    {
        [SerializeField] private float movementDuration = 0.25f;
        [SerializeField] private Ease movementAnimEase;

        public GridVector CurrentPosition { get; private set; }

        private Positioner _positioner;

        private Queue<GridVector> _positionQueue = new Queue<GridVector>();
        private Coroutine _animationRoutine;

        public void Init(Positioner positioner)
        {
            _positioner = positioner;
        }

        public void Move(GridVector toPosition, bool animated = true)
        {
            CurrentPosition = toPosition;

            if (animated)
            {
                _positionQueue.Enqueue(toPosition);

                if (_animationRoutine == null)
                {
                    _animationRoutine = StartCoroutine(RunAnimationQueueRoutine());
                }
            }
            else
            {
                transform.localPosition = _positioner.GetLocalPosition(toPosition);
            }
        }

        public void Bump(GridVector toPosition)
        {
        }

        private IEnumerator RunAnimationQueueRoutine()
        {
            YieldInstruction AnimateMove(GridVector toPosition)
            {
                Vector3 vec = _positioner.GetLocalPosition(toPosition);
                return transform
                    .DOLocalMove(vec, movementDuration)
                    .SetEase(movementAnimEase)
                    .WaitForCompletion();
            }

            while (_positionQueue.Count > 0)
            {
                GridVector pos = _positionQueue.Dequeue();
                yield return AnimateMove(pos);
            }

            _animationRoutine = null;
        }
    }
}
