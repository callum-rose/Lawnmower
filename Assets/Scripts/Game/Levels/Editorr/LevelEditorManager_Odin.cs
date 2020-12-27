using Game.Core;
using Game.Mowers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Levels.Editorr
{
#if ODIN_INSPECTOR
	public partial class LevelEditorManager
	{
		private const string Build = "Build";
		private const string BuildTabGroup = "BuildTab";
		private const string TabBuildSaved = "FromData";
		private const string TabBuildEmpty = "BuildEmpty";
		private const string Save = "Save";

		[SerializeField] [AssetsOnly] [TitleGroup(Build)]
		private MowerData mowerData;

		[SerializeField] [AssetsOnly] [TabGroup(BuildTabGroup, TabBuildSaved)]
		private LevelData levelAsset;

		[SerializeField] [Required] [TitleGroup(Save)] [InlineEditor(Expanded = true)]
		private LevelSaver levelSaver;

		private bool AppRunning => Application.isPlaying;
		private bool HasEnoughInfoToBuild => levelAsset != null && mowerData != null;

		[Button("Build")]
		[TabGroup(BuildTabGroup, TabBuildSaved)]
		[EnableIf(nameof(AppRunning))]
		[EnableIf(nameof(HasEnoughInfoToBuild))]
		private void BuildSelectedLevel()
		{
			GameSetupPassThroughData data = new GameSetupPassThroughData
			{
				Mower = mowerData,
				Level = Instantiate(levelAsset)
			};

			Begin(data, false);
		}

		[Button("Build In Edit Mode")]
		[TabGroup(BuildTabGroup, TabBuildSaved)]
		[EnableIf(nameof(AppRunning))]
		[EnableIf(nameof(HasEnoughInfoToBuild))]
		private void BuildSelectedLevelInEdit()
		{
			GameSetupPassThroughData data = new GameSetupPassThroughData
			{
				Mower = mowerData,
				Level = Instantiate(levelAsset)
			};

			Begin(data, true);
		}

		[Button(Expanded = true)]
		[TabGroup(BuildTabGroup, TabBuildEmpty)]
		[EnableIf(nameof(AppRunning))]
		private void BuildEmptyLevelInEdit(int width, int depth)
		{
			LevelData emptyLevel = ScriptableObject.CreateInstance<LevelData>();
			emptyLevel.Resize(width, depth);

			GameSetupPassThroughData data = new GameSetupPassThroughData
			{
				Mower = mowerData,
				Level = emptyLevel
			};

			Begin(data, true);
		}

		[Button]
		[EnableIf(nameof(IsEditMode))]
		[TitleGroup(Save)]
		private void SaveLevel()
		{
			levelSaver.Save_Editor(levelManager.Tiles, levelManager.MowerPosition);
		}
	}
#endif
}