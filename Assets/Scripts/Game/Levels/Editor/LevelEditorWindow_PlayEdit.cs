#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Core;
using Game.Mowers;
using Game.Mowers.Input;
using Game.Tiles;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Game.Levels.EditorWindow
{
	internal partial class LevelEditorWindow
	{
		private const string Playgroup = SplitLeft + "/Game";

		[ShowInInspector, InlineProperty, HideLabel, HideReferenceObjectPicker, BoxGroup(Playgroup)]
		private EditorWindowMowerControls _editorWindowMowerControls = new EditorWindowMowerControls();

		private MowerMovementManager _mowerMovementManager;
		internal IMowerPosition _mowerPosition;

		private List<Object> _objectsMadeForGame = new List<Object>();

		private bool _isEditMode;
		private EditableLevelData _clonedLevel;
		
		[Button, BoxGroup(Playgroup)]
		public void Begin()
		{
			Begin(false, _editableLevel.StartPosition.x, _editableLevel.StartPosition.y);
		}

		[Button, BoxGroup(Playgroup)]
		public void BeginEdit()
		{
			_tileClicked = (x, y) => Begin(true, x, y);
		}

		private void Begin(bool editMode, int x, int y)
		{
			_isEditMode = editMode;

			if (!_isEditMode)
			{
				_clonedLevel = _editableLevel;
				_editableLevel = EditableLevelData.CreateFrom(_editableLevel);
			}
			
			TileInteractor tileInteractor = CreateInstance<TileInteractor>();
			_mowerMovementManager = CreateInstance<MowerMovementManager>();
			EditModeLevelTraversalChecker levelTraversalChecker =
				AssetDatabase.LoadAssetAtPath<EditModeLevelTraversalChecker>(
					"Assets/ScriptableObjects/Game/Levels/Functionals/EditMode/EditModeLevelTraversalChecker.asset");
			EditModeLevelTileUpgrader tileUpgrader = AssetDatabase.LoadAssetAtPath<EditModeLevelTileUpgrader>(
				"Assets/ScriptableObjects/Game/Levels/Functionals/EditMode/EditModeLevelTileUpgrader.asset");
			LevelStateChecker levelStateChecker = CreateInstance<LevelStateChecker>();
			HeadlessLevelManager levelManager = CreateInstance<HeadlessLevelManager>();
			HeadlessMowerManager mowerManager = CreateInstance<HeadlessMowerManager>();

			tileInteractor.Construct(_mowerMovementManager);
			_mowerMovementManager.Construct(tileInteractor);
			levelManager.Construct(_mowerMovementManager, levelTraversalChecker, tileInteractor, levelStateChecker);
			mowerManager.Construct(_mowerMovementManager, new IMowerControls[] { _editorWindowMowerControls });

			tileInteractor.IsEditMode = editMode;
			levelTraversalChecker.IsEditMode = editMode;

			levelTraversalChecker.Init(_editableLevel);
			tileUpgrader.Init(_editableLevel);
			levelStateChecker.Init(_editableLevel, _mowerMovementManager);
			_mowerPosition = mowerManager.Init(levelTraversalChecker, _undoSystem);

			_editableLevel.StartPosition = new GridVector(x, y);
			levelManager.Init(_editableLevel);

			if (_isEditMode)
			{
				// last tile has to be this
				_editableLevel.SetTile(x, y, new GrassTile(1));
			}

			levelManager.LevelCompleted += End_Internal;
			levelManager.LevelFailed += End_Internal;

			_objectsMadeForGame = new List<Object> { tileInteractor, _mowerMovementManager, levelStateChecker, mowerManager, levelManager };
		}

		[Button, BoxGroup(Playgroup)]
		public void End()
		{
			End_Internal(_isEditMode);
		}

		private void End_Internal(Xor isInverted)
		{
			if (isInverted)
			{
				StartPosition = _mowerPosition.CurrentPosition.Value;
			}

			if (!_isEditMode)
			{
				_editableLevel = _clonedLevel;
				_clonedLevel = null;
			}

			_mowerMovementManager.IsRunning = false;
			_mowerPosition = null;

			foreach (Object o in _objectsMadeForGame)
			{
				DestroyImmediate(o);
			}
			
			Resources.UnloadUnusedAssets();
		}
	}
}
#endif