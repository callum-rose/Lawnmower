using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.UndoSystem
{
    /// <summary>Undo/redo/restart requests from the keyboard (Z/Y) and the UI buttons. Passive: it only raises events.</summary>
    internal class UndoController : MonoBehaviour
    {
        public event Action UndoRequested;
        public event Action RedoRequested;
        public event Action RestartRequested;

        #region Unity

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.zKey.wasPressedThisFrame)
            {
                Undo();
            }
            if (keyboard.yKey.wasPressedThisFrame)
            {
                Redo();
            }
        }

        #endregion

        #region API

        [ContextMenu("Undo")]
        public void Undo()
        {
            UndoRequested?.Invoke();
        }

        [ContextMenu("Redo")]
        public void Redo()
        {
            RedoRequested?.Invoke();
        }

        /// <summary>The HUD's undo-all button: restarts the level.</summary>
        [ContextMenu("Restart")]
        public void Restart()
        {
            RestartRequested?.Invoke();
        }

        #endregion
    }
}
