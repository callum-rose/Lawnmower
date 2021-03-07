using Game.Core;
using UnityEngine;
using System.Linq;

namespace Game.Mowers.Input
{
    [DefaultExecutionOrder(-99)]
    class TouchMowerControls : MonoBehaviour, IMowerControls
    {
        [SerializeField] private IMowerMovementGestureHandlerContainer[] _gestureHandlerContainers;
        [SerializeField] private MowerInputEventChannel mowerInputEventChannel;
         
        private void OnEnable()
        {
            foreach (IMowerMovementGestureHandler gh in _gestureHandlerContainers.Select(c => c.Result))
            {
                gh.Move += OnTouchGestureMoved;
            }
        }

        private void OnDisable()
        {
            foreach (IMowerMovementGestureHandler gh in _gestureHandlerContainers.Select(c => c.Result))
            {
                gh.Move -= OnTouchGestureMoved;
            }
        }

        private void OnTouchGestureMoved(GridVector direction)
        {
            mowerInputEventChannel.Raise(direction);
        }
    }
}
