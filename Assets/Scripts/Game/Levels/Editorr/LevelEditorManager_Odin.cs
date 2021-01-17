#if UNITY_EDITOR

using System.Linq;
using Game.Core;
using Game.Mowers;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

namespace Game.Levels.Editorr
{
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

		private EditableLevelData _editableLevel;
		
		[Button("Build")]
		[TabGroup(BuildTabGroup, TabBuildSaved)]
		[EnableIf(nameof(AppRunning))]
		[EnableIf(nameof(HasEnoughInfoToBuild))]
		private void BuildSelectedLevel()
		{
			GameSetupPassThroughData data = new GameSetupPassThroughData(mowerData, Instantiate(levelAsset));
			Begin(data, false);
		}

		[Button("Build In Edit Mode")]
		[TabGroup(BuildTabGroup, TabBuildSaved)]
		[EnableIf(nameof(AppRunning))]
		[EnableIf(nameof(HasEnoughInfoToBuild))]
		private void BuildSelectedLevelInEdit()
		{
			GameSetupPassThroughData data = new GameSetupPassThroughData(mowerData, EditableLevelData.CreateFrom(levelAsset));
			Begin(data, true);
		}

		[Button(Expanded = true)]
		[TabGroup(BuildTabGroup, TabBuildEmpty)]
		[EnableIf(nameof(AppRunning))]
		private void BuildEmptyLevelInEdit(int width, int depth)
		{
			LevelData emptyLevel = ScriptableObject.CreateInstance<LevelData>();

			GameSetupPassThroughData data = new GameSetupPassThroughData(mowerData, emptyLevel);
			Begin(data, true);
		}

		[Button]
		[EnableIf(nameof(IsEditMode))]
		[TitleGroup(Save)]
		private void SaveLevel()
		{
			levelSaver.Save_Editor(_editableLevel, levelManager.MowerPosition);
		}

		// private bool ValidateHasEditModeContainers(IHasEditModeContainer[] containers, ref string errorMessage)
		// {
		// 	IHasEditMode[] inspectorEditModes = containers.Select(c => c.Result).ToArray();
		// 	IHasEditMode[] foundEditModes = InterfaceHelper.FindObjects<IHasEditMode>();
		//
		// 	foreach (IHasEditMode foundEditMode in foundEditModes)
		// 	{
		// 		if (!inspectorEditModes.Contains(foundEditMode))
		// 		{
		// 			return false;
		// 		}
		// 	}
		//
		// 	return true;
		// }
		//
		// [Button]
		// private void UpdateEditModes()
		// {
		// 	hasEditModeContainers = InterfaceHelper
		// 		.FindObjects<IHasEditMode>()
		// 		.Select(i =>
		// 		{
		// 			var newContainer = new IHasEditModeContainer();
		// 			newContainer.Result = i;
		// 			return newContainer;
		// 		})
		// 		.ToArray();
		// }
	}
}
#endif