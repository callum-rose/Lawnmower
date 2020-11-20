using Core;
using Game.Core;
using Game.LevelEditor;
using Game.Mowers;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels
{
    public class LevelEditorManager : MonoBehaviour
    {
        [Space]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private MouseTileSelector tileSelector;

        [Space]
        [SerializeField] private GameObject uiContainer;

        [Space]
        [SerializeField] private GizmoGridRenderer gridRenderer;
        [SerializeField] private GizmoSelectedTileRenderer tileRenderer;

        [SerializeField, BoxGroup(nameof(IHasEditMode) + "s"), ListDrawerSettings(Expanded = true)]
        private IHasEditModeContainer[] hasEditModes;

        #region Unity

        private void Awake()
        {
            tileSelector.Selected += TileSelector_Clicked;

            levelManager.LevelChanged += LevelManager_LevelChanged;
        }

        private void OnDestroy()
        {
            tileSelector.Selected -= TileSelector_Clicked;

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

        #region Odin
#if UNITY_EDITOR

        [SerializeField, AssetsOnly, BoxGroup("Debug")] private LevelData levelAsset;
        [SerializeField, AssetsOnly, BoxGroup("Debug")] private MowerData mowerData;

        private bool AppRunning => Application.isPlaying;

        [Button("Build"), BoxGroup("Debug"), EnableIf(nameof(AppRunning))]
        private void BuildSelectedLevel()
        {
            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = mowerData.Id,
                Level = levelAsset.GetCopy()
            };

            gameManager.Begin(data);

            uiContainer.SetActive(false);

            SetEditMode(false);
        }

        [Button("Build In Edit Mode"), BoxGroup("Debug"), EnableIf(nameof(AppRunning))]
        private void BuildSelectedLevelInEdit()
        {
            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = mowerData.Id,
                Level = levelAsset.GetCopy()
            };

            gameManager.Begin(data);

            uiContainer.SetActive(true);

            SetEditMode(true);
        }

        [Button(Expanded = true), BoxGroup("Debug"), EnableIf(nameof(AppRunning))]
        private void BuildEmptyLevelInEdit(int width, int depth)
        {
            var emptyLevel = ScriptableObject.CreateInstance<LevelData>();
            emptyLevel.Resize(width, depth);

            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = mowerData.Id,
                Level = emptyLevel
            };

            gameManager.Begin(data);

            uiContainer.SetActive(true);

            SetEditMode(true);
        }

#endif
        #endregion

        #region Methods

        private void SetEditMode(bool isEditMode)
        {
            foreach (var e in hasEditModes)
            {
                e.Result.IsEditMode = isEditMode;
            }
        }

        #endregion
    }
}
