using Game.Core;
using Game.Mowers;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using R = Sirenix.OdinInspector.RequiredAttribute;

namespace Game.Levels.Editorr
{
    public partial class LevelEditorManager : MonoBehaviour, IHasEditMode
    {
        [Space]
        [SerializeField, R] private GameManager gameManager;
        [SerializeField, R] private LevelManager levelManager;
        [SerializeField, R] private ITileSelectorContainer tileSelector;

        [Space]
        [SerializeField, R] private GameObject uiContainer;

        [Space]
        [SerializeField, R] private GizmoGridRenderer gridRenderer;
        [SerializeField, R] private GizmoSelectedTileRenderer tileRenderer;

        [SerializeField, R, ListDrawerSettings]
        private IHasEditModeContainer[] hasEditModes;

        public bool IsEditMode
        {
            get => ___isEditMode;
            set
            {
                ___isEditMode = value;

                foreach (var e in hasEditModes)
                {
                    e.Result.IsEditMode = ___isEditMode;
                }
            }
        }

        private bool ___isEditMode;

        #region Unity

        private void Awake()
        {
            tileSelector.Result.Selected += TileSelector_Clicked;

            levelManager.LevelChanged += LevelManager_LevelChanged;
        }

        private void OnDestroy()
        {
            tileSelector.Result.Selected -= TileSelector_Clicked;

            levelManager.LevelChanged -= LevelManager_LevelChanged;
        }

        #endregion

        #region Events

        private void TileSelector_Clicked(GridVector position)
        {
            tileRenderer.X = position.x;
            tileRenderer.Y = position.y;
        }

        private void LevelManager_LevelChanged()
        {
            gridRenderer.Width = levelManager.Level.Width;
            gridRenderer.Depth = levelManager.Level.Depth;
        }

        #endregion
        
        #region Methods

        private void Begin(GameSetupPassThroughData data, bool isEdit)
        {
            gameManager.Begin(data);

            uiContainer.SetActive(isEdit);

            IsEditMode = isEdit;

            InterfaceHelper.FindObject<IMowerRunnable>().IsRunning = !isEdit;
        }

        #endregion
    }
}
