using Sirenix.OdinInspector;
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

        [Button]
        public void Undo()
        {
            UndoManager.Undo();
        }

        [Button]
        public void Redo()
        {
            UndoManager.Redo();
        }

        #endregion
    }
}
