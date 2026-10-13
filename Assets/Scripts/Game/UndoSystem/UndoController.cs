using UnityEngine;

namespace Game.UndoSystem
{
    internal class UndoController : MonoBehaviour
    {
        private IUndoSystem UndoManager { get; set; }

        public bool IsRunning { get; set; }

        public void Bind(IUndoSystem undoSystem)
        {
            UndoManager = undoSystem;
        }

        #region Unity

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
        }

        #endregion

        #region API

        [ContextMenu("Undo")]
        public void Undo()
        {
            UndoManager?.Undo();
        }

        [ContextMenu("Undo All")]
        public void UndoAll()
        {
            if (UndoManager == null)
            {
                return;
            }

            while (UndoManager.Undo())
            {
            }
        }

        [ContextMenu("Redo")]
        public void Redo()
        {
            UndoManager?.Redo();
        }

        #endregion
    }
}
