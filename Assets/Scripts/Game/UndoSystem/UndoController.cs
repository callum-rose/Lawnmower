using UnityEngine;

namespace Game.UndoSystem
{
    internal class UndoController : MonoBehaviour
    {
        [SerializeField] private IUndoSystemContainer undoManagerContainer;

        private IUndoSystem UndoManager => undoManagerContainer.Result;

        public bool IsRunning { get; set; }

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
            UndoManager.Undo();
        }

        [ContextMenu("Undo All")]
        public void UndoAll()
        {
            for (int i = 0; i < UndoManager.Count; i++)
            {
                UndoManager.Undo();
            }
        }

        [ContextMenu("Redo")]
        public void Redo()
        {
            UndoManager.Redo();
        }

        #endregion
    }
}
