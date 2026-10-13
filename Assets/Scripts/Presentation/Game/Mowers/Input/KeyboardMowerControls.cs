using Game.Core;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class KeyboardMowerControls : MonoBehaviour, IMowerControls
    {
        [SerializeField] private MowerInputEventChannel mowerInputEventChannel;
         
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                mowerInputEventChannel.Raise(GridVector.Right);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                mowerInputEventChannel.Raise(GridVector.Left);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.W) || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                mowerInputEventChannel.Raise(GridVector.Up);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.S) || UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                mowerInputEventChannel.Raise(GridVector.Down);
            }
        }
    }
}
