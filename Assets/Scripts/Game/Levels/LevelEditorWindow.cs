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
		private LevelData _levelBaseOn;

		[BoxGroup(SplitLeft), ReadOnly, ShowInInspector]
		private EditableLevelData _clonedCurrent;

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

		private Tile _currentTilePaint = new GrassTile(2);

		private IUndoSystem _undoSystem;
		
		private delegate void TileClickedEvent(int x, int y);
		private TileClickedEvent _tileClicked;

		public static void OpenWindow(LevelData levelData)
		{
			LevelEditorWindow window = GetWindow<LevelEditorWindow>();
			window.Show();
			window.SetLevel(levelData);
			window.Initialize();
		}

		private void SetLevel(LevelData levelData)
		{
			_levelBaseOn = levelData;
			SaveCurrentLevelPath();
			CloneLevel();
		}

		protected override void Initialize()
		{
			WindowPadding = Vector4.zero;

			if (_levelBaseOn == null)
			{
				string path = LoadLastSavedLevelPath();
				_levelBaseOn = AssetDatabase.LoadAssetAtPath<LevelData>(path);
			}

			_undoSystem = new UndoSystem.UndoSystem();
			
			_tiles = new InspectorLevelDataWrapper(this);
			_tileTypeButtons = new TileTypeButtons(this);
		}

		private void CloneLevel()
		{
			_clonedCurrent = EditableLevelData.CreateFrom(_levelBaseOn);
			Debug.Log("Cloned " + _levelBaseOn.name);
		}

		private void SaveCurrentLevelPath()
		{
			const string key = "LevelEditorWindow_LevelAssetPath";
			EditorPrefs.SetString(key, AssetDatabase.GetAssetPath(_levelBaseOn));
		}

		private string LoadLastSavedLevelPath()
		{
			const string key = "LevelEditorWindow_LevelAssetPath";
			return EditorPrefs.GetString(key, null);
		}

		private static Color GetColourForTile(Tile tileData)
		{
			Color colour;
			switch (tileData)
			{
				case EmptyTile _:
					colour = Color.white;
					break;
				
				case GrassTile grassTile:
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
					break;
				
				case StoneTile _:
					colour = Color.gray;
					break;
				
				case WaterTile _:
					colour = Color.blue;
					break;
				
				case WoodTile _:
					ColorUtility.TryParseHtmlString("#916B4C", out colour);
					break;
				
				default:
					throw new ArgumentOutOfRangeException();
			}

			return colour;
		}

		[Button, BoxGroup(SplitLeft)]
		private void ClickSetStartPosition()
		{
			_tileClicked = (x, y) => _clonedCurrent.StartPosition = new GridVector(x, y);
		}

		[Button, BoxGroup(SplitLeft)]
		private void SaveLevel()
		{
			LevelData levelData = LevelData.CreateFrom(_clonedCurrent);
			
			AssetDatabase.CreateAsset(levelData, AssetDatabase.GetAssetPath(_levelBaseOn));
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			_levelBaseOn = levelData;
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
		
		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandUp()
		{
			_clonedCurrent.ExpandUp();
		}

		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandRight()
		{
			_clonedCurrent.ExpandRight();
		}
		
		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandDown()
		{
			_clonedCurrent.ExpandDown();
		}
		
		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandLeft()
		{			
			_clonedCurrent.ExpandLeft();
		}

		private void OnTileClicked(int x, int y)
		{
			_tileClicked ??= SetTile;
			_tileClicked.Invoke(x, y);
			_tileClicked = null;
		}

		private void SetTile(int x, int y)
		{
			Tile currentTile = _clonedCurrent.GetTile(x, y);

			void Set_Local(Tile tile)
			{
				_clonedCurrent.SetTile(x, y, tile);
			}

			IUndoable undoable = new Undoable(
				() => Set_Local(_currentTilePaint),
				() => Set_Local(currentTile));

			_undoSystem.Do(undoable);
		}
	}
}