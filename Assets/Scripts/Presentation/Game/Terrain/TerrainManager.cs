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
	/// colour (grass height, dirt, ...) goes in a one-texel-per-tile texture the terrain shader blends between, with how
	/// much of it is dirt in alpha. The dirt is raked along a <see cref="RakePath"/> through the grass tiles.
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
		private static readonly int RakeId = Shader.PropertyToID("_TerrainRake");

		private readonly List<Action> _unsubscribes = new List<Action>();

		private Level _level;
		private Mesh _mesh;
		private Texture2D _cells;
		private Texture2D _rake;
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

			if (_rake)
			{
				Destroy(_rake);
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
				edging.Build(footprint, settings.TopHeight);
			}
			finally
			{
				footprint.Dispose();
			}

			transform.localScale = new Vector3(LevelDimensions.TileSize, 1, LevelDimensions.TileSize);
		}

		private void BuildCells(Level level)
		{
			if (_cells == null || _cells.width != level.Width || _cells.height != level.Depth)
			{
				if (_cells)
				{
					Destroy(_cells);
				}

				_cells = new Texture2D(level.Width, level.Depth, TextureFormat.RGBA32, false, false)
				{
					name = "TerrainCells",
					filterMode = FilterMode.Bilinear,
					wrapMode = TextureWrapMode.Clamp,
				};
			}

			foreach (GridVector position in level.Positions)
			{
				Tile tile = level.GetTile(position);
				_cells.SetPixel(position.x, position.y, GetTopColour(tile));

				if (tile is IReadonlyGrassTile grass)
				{
					GridVector cell = position;
					UndoableAction<int, int> onHeightChanged = (_, height, _) => SetCell(cell, GrassColour(height));
					grass.GrassHeight.ValueChangedFromTo += onHeightChanged;
					_unsubscribes.Add(() => grass.GrassHeight.ValueChangedFromTo -= onHeightChanged);
				}
			}

			_cells.Apply();
			BuildRake(level);

			_renderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetTexture(CellsId, _cells);
			_propertyBlock.SetTexture(RakeId, _rake);
			_renderer.SetPropertyBlock(_propertyBlock);
		}

		private void BuildRake(Level level)
		{
			if (_rake == null || _rake.width != level.Width || _rake.height != level.Depth)
			{
				if (_rake)
				{
					Destroy(_rake);
				}

				_rake = new Texture2D(level.Width, level.Depth, TextureFormat.RGBA32, false, true)
				{
					name = "TerrainRake",
					filterMode = FilterMode.Point,
					wrapMode = TextureWrapMode.Clamp,
				};
			}

			bool[,] rakeable = new bool[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				rakeable[position.x, position.y] = level.GetTile(position).Kind == TileKind.Grass;
			}

			(byte a, byte b)[,] sides = RakePath.Build(rakeable);
			Color32[] pixels = new Color32[level.Width * level.Depth];
			for (int z = 0; z < level.Depth; z++)
			{
				for (int x = 0; x < level.Width; x++)
				{
					pixels[z * level.Width + x] = new Color32(sides[x, z].a, sides[x, z].b, 0, 0);
				}
			}

			_rake.SetPixels32(pixels);
			_rake.Apply();
		}

		private void SetCell(GridVector position, Color colour)
		{
			_cells.SetPixel(position.x, position.y, colour);
			_cells.Apply();
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
				_ when tile.Kind == TileKind.Spring => WithoutDirt(springColour),
				_ => WithoutDirt(stoneColour),
			};
		}

		private Color GrassColour(int height)
		{
			Color colour = grassColours[Mathf.Clamp(height, 0, grassColours.Length - 1)];
			colour.a = height <= 0 ? 1 : 0;
			return colour;
		}

		private static Color WithoutDirt(Color colour)
		{
			colour.a = 0;
			return colour;
		}

		internal static bool IsSolid(TileKind kind)
		{
			return kind is TileKind.Grass or TileKind.Stone or TileKind.Spring;
		}

		#endregion
	}
}
