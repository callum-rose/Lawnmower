using Game.Tiles;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using System;
using Game.Core;
using Game.UndoSystem;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Utils;

namespace Game.Levels
{
	internal partial class LevelEditorWindow : OdinEditorWindow
	{
		private const string Split = "Split";
		private const string SplitLeft = Split + "/Left";
		private const string SplitRight = Split + "/Tiles";

		[HorizontalGroup(Split, 300, LabelWidth = 100)]
		[BoxGroup(SplitLeft), SerializeField,
		 InfoBox("Select a " + nameof(LevelData) + " asset", nameof(CurrentNull),
			 InfoMessageType = InfoMessageType.Warning), OnValueChanged(nameof(CloneLevel))]
		private LevelData _current;

		[BoxGroup(SplitLeft), ReadOnly, ShowInInspector]
		private LevelData _clonedCurrent;

		[BoxGroup(SplitLeft), ShowInInspector, DisplayAsString, LabelText("Current Selection:")] 
		private string TileDataString => _currentTilePaint.ToString();

		[ShowInInspector, BoxGroup(SplitLeft), InlineProperty, HideLabel] private TileTypeButtons _tileTypeButtons;

		[BoxGroup(SplitRight), ShowIf(nameof(CurrentNotNull)), ShowInInspector]
		private InspectorLevelDataWrapper _tiles;

		[ShowInInspector, BoxGroup(SplitLeft), InlineProperty]
		private GridVector StartPosition
		{
			get => _clonedCurrent != null ? _clonedCurrent.StartPosition : GridVector.Zero;
			set
			{
				_clonedCurrent.StartPosition = value;
				_clonedCurrent.ValidateStartPos();
			}
		}
		
		private bool CurrentNotNull => _clonedCurrent != null;
		private bool CurrentNull => _clonedCurrent == null;

		private Tilee _currentTilePaint = new GrassTile(2);

		private IUndoSystem _undoSystem;

		public static void OpenWindow(LevelData levelData)
		{
			LevelEditorWindow window = GetWindow<LevelEditorWindow>();
			window.Show();
			window.SetLevel(levelData);
			window.Initialize();
		}

		private void SetLevel(LevelData levelData)
		{
			_current = levelData;
			SaveCurrentLevelPath();
			CloneLevel();
		}

		protected override void Initialize()
		{
			WindowPadding = Vector4.zero;

			if (_current == null)
			{
				string path = LoadLastSavedLevelPath();
				_current = AssetDatabase.LoadAssetAtPath<LevelData>(path);
			}

			_undoSystem = new UndoSystem.UndoSystem();
			
			_tiles = new InspectorLevelDataWrapper(this, _undoSystem);
			_tileTypeButtons = new TileTypeButtons(this);
		}

		private void CloneLevel()
		{
			_clonedCurrent = Instantiate(_current);
			Debug.Log("Cloned " + _clonedCurrent.name);
		}

		private void SaveCurrentLevelPath()
		{
			const string key = "LevelEditorWindow_LevelAssetPath";
			EditorPrefs.SetString(key, AssetDatabase.GetAssetPath(_current));
		}

		private string LoadLastSavedLevelPath()
		{
			const string key = "LevelEditorWindow_LevelAssetPath";
			return EditorPrefs.GetString(key, null);
		}

		private static Color GetColourForTile(Tilee tileData)
		{
			Color colour;
			if (tileData is EmptyTile)
			{
				colour = Color.white;
			}
			else if (tileData is GrassTile grassTile)
			{
				switch (grassTile.GrassHeight.Value)
				{
					case 3:
						ColorUtility.TryParseHtmlString("#0D7352", out colour);
						break;
					case 2:
						ColorUtility.TryParseHtmlString("#12A175", out colour);
						break;
					case 1:
						ColorUtility.TryParseHtmlString("#1BF1AC", out colour);
						break;
					default:
						colour = Color.black;
						break;
				}
			}
			else if (tileData is StoneTile)
			{
				colour = Color.gray;
			}
			else if (tileData is WaterTile)
			{
				colour = Color.blue;
			}
			else if (tileData is WoodTile)
			{
				ColorUtility.TryParseHtmlString("#916B4C", out colour);
			}
			else
			{
				throw new ArgumentOutOfRangeException();
			}

			return colour;
		}

		[Button, BoxGroup(SplitLeft)]
		private void SaveLevel()
		{
			AssetDatabase.CreateAsset(_clonedCurrent, AssetDatabase.GetAssetPath(_current));
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			_current = _clonedCurrent;
			CloneLevel();
		}

		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/UndoButtons")]
		private void Undo()
		{
			_undoSystem.Undo();
		}
		
		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/UndoButtons")]
		private void Redo()
		{
			_undoSystem.Redo();
		}
	}
}