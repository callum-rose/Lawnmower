using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core;
using Game.Levels;
using Game.Tiles;
using Game.UndoSystem;
using Lawnmower.Model;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
	/// <summary>
	/// Builds one smooth terrain mesh under each level from an SDF of its solid tiles, in place of a block per tile.
	/// The mesh sits in the level container's space, where tile (x, z) covers [x, x + 1] x [z, z + 1]. Each tile's top
	/// colour (grass height, dirt, ...) goes in a one-texel-per-tile texture the terrain shader blends between, with a
	/// one-texel border; empty tiles and the border have zero alpha, which the shader fills with its shore colour.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	internal class TerrainManager : MonoBehaviour
	{
		[SerializeField] private TerrainSettings settings;
		[SerializeField] private GamePlayData gamePlayData;
		[SerializeField] private TerrainEdging edging;

		[Header("Top Colours")]
		[Tooltip("Indexed by grass height: ruined (0) to tallest (3)")]
		[SerializeField] private Color[] grassColours =
		{
			new Color(0.36f, 0.22f, 0.12f),
			new Color(0.05f, 0.20f, 0.08f),
			new Color(0.16f, 0.22f, 0.06f),
			new Color(0.32f, 0.32f, 0.13f),
		};

		[SerializeField] private Color stoneColour = new Color(0.05f, 0.20f, 0.08f);
		[SerializeField] private Color springColour = new Color(0.36f, 0.22f, 0.12f);

		[Header("Intro")]
		[SerializeField] private Vector3 introOffset = new Vector3(0, -2, 0);
		[SerializeField] private Ease introEase = Ease.OutCubic;

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelStartedEventChannel;

		private static readonly int CellsId = Shader.PropertyToID("_TerrainCells");

		private readonly List<Action> _unsubscribes = new List<Action>();

		private Level _level;
		private Mesh _mesh;
		private Texture2D _cells;
		private MeshRenderer _renderer;
		private MaterialPropertyBlock _propertyBlock;

		#region Unity

		private void Awake()
		{
			_mesh = new Mesh { name = "Terrain" };
			_mesh.MarkDynamic();
			GetComponent<MeshFilter>().sharedMesh = _mesh;
			_renderer = GetComponent<MeshRenderer>();
			_propertyBlock = new MaterialPropertyBlock();

			levelStartedEventChannel.EventRaised += OnLevelStarted;
#if UNITY_EDITOR
			settings.Changed += OnSettingsChanged;
#endif
		}

		private void OnDestroy()
		{
			levelStartedEventChannel.EventRaised -= OnLevelStarted;
#if UNITY_EDITOR
			settings.Changed -= OnSettingsChanged;
#endif
			Unsubscribe();

			Destroy(_mesh);
			if (_cells)
			{
				Destroy(_cells);
			}
		}

		#endregion

		#region Events

		private void OnLevelStarted(Level level)
		{
			_level = level;
			Unsubscribe();
			Build(level);
			BuildCells(level);
			PlayIntro();
		}

#if UNITY_EDITOR
		private void OnSettingsChanged()
		{
			if (_level != null)
			{
				Build(_level);
			}
		}
#endif

		#endregion

		#region Methods

		private void Build(Level level)
		{
			bool[,] solid = new bool[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				solid[position.x, position.y] = IsSolid(level.GetTile(position).Kind);
			}

			TerrainFootprint footprint = TerrainFootprint.Create(solid, settings.ConvexCornerRadius,
				settings.ConcaveCornerRadius, Allocator.TempJob);

			try
			{
				float margin = settings.Margin;
				float3 min = new float3(-margin, settings.BottomHeight, -margin);
				float3 max = new float3(level.Width + margin, settings.TopHeight + settings.VoxelSize * 2,
					level.Depth + margin);

				TerrainMesher.Build(_mesh, footprint, settings.Shape, min, max, settings.VoxelSize);
				edging.Build(footprint, settings.TopHeight, settings.ShoreWidth);
			}
			finally
			{
				footprint.Dispose();
			}

			transform.localScale = new Vector3(LevelDimensions.TileSize, 1, LevelDimensions.TileSize);
		}

		private void BuildCells(Level level)
		{
			int width = level.Width + 2;
			int depth = level.Depth + 2;
			if (_cells == null || _cells.width != width || _cells.height != depth)
			{
				if (_cells)
				{
					Destroy(_cells);
				}

				_cells = new Texture2D(width, depth, TextureFormat.RGBA32, false, false)
				{
					name = "TerrainCells",
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp,
				};
			}

			_cells.SetPixels32(new Color32[width * depth]);

			foreach (GridVector position in level.Positions)
			{
				Tile tile = level.GetTile(position);
				SetCell(position, GetTopColour(tile), false);

				if (tile is IReadonlyGrassTile grass)
				{
					GridVector cell = position;
					UndoableAction<int, int> onHeightChanged = (_, height, _) => SetCell(cell, GrassColour(height), true);
					grass.GrassHeight.ValueChangedFromTo += onHeightChanged;
					_unsubscribes.Add(() => grass.GrassHeight.ValueChangedFromTo -= onHeightChanged);
				}
			}

			_cells.Apply();

			_renderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetTexture(CellsId, _cells);
			_renderer.SetPropertyBlock(_propertyBlock);
		}

		private void SetCell(GridVector position, Color colour, bool apply)
		{
			_cells.SetPixel(position.x + 1, position.y + 1, colour);
			if (apply)
			{
				_cells.Apply();
			}
		}

		private void PlayIntro()
		{
			transform.DOKill();
			transform.localPosition = introOffset;
			transform.DOLocalMove(Vector3.zero, gamePlayData.LevelIntroDuration).SetEase(introEase);
		}

		private void Unsubscribe()
		{
			foreach (Action unsubscribe in _unsubscribes)
			{
				unsubscribe();
			}

			_unsubscribes.Clear();
		}

		private Color GetTopColour(Tile tile)
		{
			return tile switch
			{
				IReadonlyGrassTile grass => GrassColour(grass.GrassHeight.Value),
				_ when tile.Kind == TileKind.Spring => springColour,
				_ when IsSolid(tile.Kind) => stoneColour,
				_ => Color.clear,
			};
		}

		private Color GrassColour(int height)
		{
			return grassColours[Mathf.Clamp(height, 0, grassColours.Length - 1)];
		}

		internal static bool IsSolid(TileKind kind)
		{
			return kind is TileKind.Grass or TileKind.Stone or TileKind.Spring;
		}

		#endregion
	}
}
