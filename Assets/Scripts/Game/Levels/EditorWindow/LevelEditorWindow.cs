using System;
using System.IO;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Utils;

namespace Game.Levels.EditorWindow
{
	internal partial class LevelEditorWindow : OdinEditorWindow
	{
		private const string Split = "Split";
		private const string SplitLeft = Split + "/Left";
		private const string SplitRight = Split + "/Tiles";

		private string _levelBasedOnPath;

		[HorizontalGroup(Split, 300, LabelWidth = 100)]
		[BoxGroup(SplitLeft)]
		[Button]
		private void CreateEmptyLevel([MinValue(1)] int width, [MinValue(1)] int height)
		{
			EditableLevelData levelData = new EditableLevelData();
			Loops.TwoD(width, height, (x, y) => levelData.SetTile(x, y, new EmptyTile()));
			levelData.StartPosition = GridVector.Zero;
			SetLevel(levelData, null);
		}

		private EditableLevelData _editableLevel;

		[BoxGroup(SplitLeft), ShowInInspector, DisplayAsString, LabelText("Current Selection:")]
		private string TileDataString => _currentTilePaint.ToString();

		[ShowInInspector, BoxGroup(SplitLeft), InlineProperty, HideLabel, HideReferenceObjectPicker]
		private TileTypeButtons _tileTypeButtons;

		[BoxGroup(SplitRight), ShowIf(nameof(CurrentNotNull)), ShowInInspector, HideLabel, HideReferenceObjectPicker]
		private InspectorLevelDataWrapper _tiles;

		[ShowInInspector, BoxGroup(SplitLeft), InlineProperty]
		private GridVector StartPosition
		{
			get => _editableLevel != null ? _editableLevel.StartPosition : GridVector.Zero;
			set
			{
				_editableLevel.StartPosition = value;
				_editableLevel.ValidateStartPos();
			}
		}

		private bool CurrentNotNull => _editableLevel != null;
		private bool CurrentNull => _editableLevel == null;

		private Tile _currentTilePaint = new GrassTile(2);

		private IUndoSystem _undoSystem;

		private delegate void TileClickedEvent(int x, int y);

		private TileClickedEvent _tileClicked;

		public static void OpenWindow(LevelData levelData)
		{
			LevelEditorWindow window = GetWindow<LevelEditorWindow>();
			window.Show();
			window.SetLevel(levelData, AssetDatabase.GetAssetPath(levelData));
			window.Initialize();
		}

		private void SetLevel(IReadOnlyLevelData levelData, string assetPath)
		{
			_levelBasedOnPath = assetPath;

			MakeEditableVersionOfLevel(levelData);
		}

		protected override void Initialize()
		{
			WindowPadding = Vector4.zero;

			_undoSystem = new UndoSystem.UndoSystem();

			_tiles = new InspectorLevelDataWrapper(this);
			_tileTypeButtons = new TileTypeButtons(this);
		}

		private void MakeEditableVersionOfLevel(IReadOnlyLevelData levelData)
		{
			_editableLevel = EditableLevelData.CreateFrom(levelData);
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
			_tileClicked = (x, y) => _editableLevel.StartPosition = new GridVector(x, y);
		}

		[Button, BoxGroup(SplitLeft)]
		private void TrimLevel()
		{
			_editableLevel.Trim();
		}

		[Button, BoxGroup(SplitLeft)]
		private void SaveLevel()
		{
			LevelData levelData = LevelData.CreateFrom(_editableLevel);

			string basedOnAssetAtPath = !string.IsNullOrEmpty(_levelBasedOnPath)
				? Path.GetFullPath(_levelBasedOnPath)
				: @"C:/Users/callu/Documents/GitHub/Lawnmower/Assets/ScriptableObjects/Game/Levels/Data/";

			string path = EditorUtility.SaveFilePanel("Save Level Asset", basedOnAssetAtPath, "NewLevel", "asset");

			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			path = path.Replace(@"C:/Users/callu/Documents/GitHub/Lawnmower/", "");

			AssetDatabase.CreateAsset(levelData, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			SetLevel(levelData, path);
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
			_editableLevel.ExpandUp();
		}

		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandRight()
		{
			_editableLevel.ExpandRight();
		}

		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandDown()
		{
			_editableLevel.ExpandDown();
		}

		[Button, BoxGroup(SplitLeft), HorizontalGroup(SplitLeft + "/Expand Level")]
		private void ExpandLeft()
		{
			_editableLevel.ExpandLeft();
		}

		private void OnTileClicked(int x, int y)
		{
			_tileClicked ??= SetTile;
			_tileClicked.Invoke(x, y);
			_tileClicked = null;
		}

		private void SetTile(int x, int y)
		{
			Tile currentTile = _editableLevel.GetTile(x, y);

			void Set_Local(Tile tile)
			{
				_editableLevel.SetTile(x, y, tile);
			}

			IUndoable undoable = new Undoable(
				() => Set_Local(_currentTilePaint.Clone()),
				() => Set_Local(currentTile));

			_undoSystem.Do(undoable);
		}
	}
}