using Game.Core;
using System;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class KeyboardMowerControls : MonoBehaviour, IMowerControls
    {
        public event Action<GridVector> MovedInDirection;

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.D) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                MovedInDirection?.Invoke(GridVector.Right);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.A) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                MovedInDirection?.Invoke(GridVector.Left);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.W) || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                MovedInDirection?.Invoke(GridVector.Up);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.S) || UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                MovedInDirection?.Invoke(GridVector.Down);
            }
        }
    }
}
