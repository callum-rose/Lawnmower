using Game.Core;
using Game.Levels;
using Game.Mowers;
using Game.Tiles;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using R = Sirenix.OdinInspector.RequiredAttribute;

namespace Game.Levels.Editor
{
    public class LevelEditorManager : MonoBehaviour, IHasEditMode
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

        #region Odin
#if UNITY_EDITOR

        [SerializeField, R, AssetsOnly, BoxGroup(Build)] private LevelData levelAsset;
        [SerializeField, R, AssetsOnly, BoxGroup(Build)] private MowerData mowerData;

        private const string Build = "Build";
        private const string Save = "Save";

        private bool AppRunning => Application.isPlaying;

        [Button("Build"), BoxGroup(Build), EnableIf(nameof(AppRunning))]
        private void BuildSelectedLevel()
        {
            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = mowerData.Id,
                Level = levelAsset.GetCopy()
            };

            Begin(data, false);

        }

        [Button("Build In Edit Mode"), BoxGroup(Build), EnableIf(nameof(AppRunning))]
        private void BuildSelectedLevelInEdit()
        {
            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = mowerData.Id,
                Level = levelAsset.GetCopy()
            };

            Begin(data, true);
        }

        [Button(Expanded = true), BoxGroup(Build), EnableIf(nameof(AppRunning))]
        private void BuildEmptyLevelInEdit(int width, int depth)
        {
            var emptyLevel = ScriptableObject.CreateInstance<LevelData>();
            emptyLevel.Resize(width, depth);

            GameSetupPassThroughData data = new GameSetupPassThroughData
            {
                MowerId = mowerData.Id,
                Level = emptyLevel
            };

            Begin(data, true);
        }

        [SerializeField, R, BoxGroup(Save), InlineEditor(Expanded = true)] private LevelSaver levelSaver;

        [Button, EnableIf(nameof(IsEditMode)), BoxGroup(Save)]
        private void SaveLevel()
        {
            levelSaver.Save_Editor(levelManager.Tiles, levelManager.MowerPosition);
        }

#endif
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
