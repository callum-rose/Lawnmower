using Game.Core;
using Game.Mowers;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;
using R = Sirenix.OdinInspector.RequiredAttribute;

namespace Game.Levels.Editorr
{
	public partial class LevelEditorManager : MonoBehaviour, IHasEditMode
	{
		[TitleGroup("Dependencies")]
		[SerializeField, R] private GameManager gameManager;

		[SerializeField, R] private HeadlessLevelManager levelManager;
		[SerializeField, R] private ITileSelectorContainer tileSelector;
		[SerializeField, R] private EditModeLevelTraversalChecker levelTraversalChecker;

		[Space]
		[SerializeField, R] private GameObject uiContainer;

		[Space]
		[SerializeField, R] private GizmoGridRenderer gridRenderer;

		[SerializeField, R] private GizmoSelectedTileRenderer tileRenderer;

		[FormerlySerializedAs("hasEditModes"), SerializeField, R, ListDrawerSettings, LabelWidth(0)]
		//[ValidateInput(nameof(ValidateHasEditModeContainers))]
		private IHasEditModeContainer[] hasEditModeContainers;

		public bool IsEditMode
		{
			get => ___isEditMode;
			set
			{
				___isEditMode = value;

				foreach (IHasEditModeContainer e in hasEditModeContainers)
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

		private void OnEnable()
		{
#if UNITY_EDITOR
			if (autoBuild && Time.frameCount == 0)
			{
				BuildSelectedLevel();
			}
#endif
		}

		private void OnDestroy()
		{
			if (tileSelector.Result != null)
			{
				tileSelector.Result.Selected -= TileSelector_Clicked;
			}

			if (levelManager != null)
			{
				levelManager.LevelChanged -= LevelManager_LevelChanged;
			}
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