using System;
using System.Collections.Generic;
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
	/// The mesh sits in the level container's space, where tile (x, z) covers [x, x + 1] x [z, z + 1]. Its top is raked
	/// dirt throughout, along a <see cref="RakePath"/> through the grass tiles. The rake goes round the obstacles
	/// (stones and springs), which are circled by rings of their own, as is each of the little mushrooms that mark where
	/// a spring lands. Where there's grass, the top is tinted by its height from a one-texel-per-tile texture the
	/// terrain shader blends between, with how much of it is dirt in alpha, inside a one-texel border; everywhere else,
	/// including mown-out grass, is bare dirt. The tint makes the grass look thicker, so it fades in as the grass grows
	/// in the level intro.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	internal class TerrainManager : MonoBehaviour
	{
		[SerializeField] private TerrainSettings settings;
		[SerializeField] private TerrainEdging edging;

		[Header("Grass Tint")]
		[Tooltip("Under grass, indexed by grass height: ruined (0, unused: ruined grass is bare dirt) to tallest (3)")]
		[SerializeField] private Color[] grassColours =
		{
			new Color(0.36f, 0.22f, 0.12f),
			new Color(0.05f, 0.20f, 0.08f),
			new Color(0.16f, 0.22f, 0.06f),
			new Color(0.32f, 0.32f, 0.13f),
		};

		[Header("Obstacles")]
		[Tooltip("Half the width of the ground a stone stands on, in tiles. The rake goes round it")]
		[SerializeField, Range(0, 0.5f)] private float stoneFootprint = 0.42f;
		[Tooltip("Half the width of the ground a spring's stem stands on, in tiles. The rake goes round it")]
		[SerializeField, Range(0, 0.5f)] private float springFootprint = 0.25f;
		[Tooltip("Where the little mushrooms of a spring's landing marker stand, from the middle of its tile, in tiles. " +
			"Matches spring_landing_voxel")]
		[SerializeField] private Vector2[] landingMarkerStems =
		{
			new Vector2(0.3f, -0.3f), new Vector2(0.3f, 0.3f), new Vector2(-0.3f, -0.3f), new Vector2(-0.3f, 0.3f),
		};
		[Tooltip("Radius of the ground each landing marker mushroom covers, in tiles. The rake circles it")]
		[SerializeField, Range(0, 0.3f)] private float landingMarkerFootprint = 0.1f;

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelStartedEventChannel;

		private static readonly int CellsId = Shader.PropertyToID("_TerrainCells");
		private static readonly int RakeId = Shader.PropertyToID("_TerrainRake");
		private static readonly int CirclesId = Shader.PropertyToID("_RakeCircles");
		private static readonly int CircleCountId = Shader.PropertyToID("_RakeCircleCount");

		// As many as the terrain shader's _RakeCircles holds
		private const int MaxCircles = 32;

		private readonly List<Action> _unsubscribes = new List<Action>();

		private Level _level;
		private Mesh _mesh;
		private Texture2D _cells;
		private Texture2D _rake;
		private readonly Vector4[] _circles = new Vector4[MaxCircles];
		private int _circleCount;
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
			BuildRake(level);
			BuildCells(level);
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
			bool[,] holes = new bool[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				TileKind kind = level.GetTile(position).Kind;
				solid[position.x, position.y] = IsSolid(kind);
				holes[position.x, position.y] = kind == TileKind.RabbitHole;
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
				edging.Build(footprint, settings.TopHeight, settings.ShoreWidth, holes);
			}
			finally
			{
				footprint.Dispose();
			}

			transform.localScale = new Vector3(LevelDimensions.TileSize, 1, LevelDimensions.TileSize);
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

			// The rake can't go through an obstacle, so it only crosses the solid tiles that are grass
			bool[,] rakeable = new bool[level.Width, level.Depth];
			byte[,] footprints = new byte[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				TileKind kind = level.GetTile(position).Kind;
				rakeable[position.x, position.y] = IsSolid(kind) && Footprint(kind) <= 0;
				footprints[position.x, position.y] = (byte)Mathf.RoundToInt(Footprint(kind) * 255);
			}

			(byte a, byte b)[,] sides = RakePath.Build(rakeable);
			Color32[] pixels = new Color32[level.Width * level.Depth];
			for (int z = 0; z < level.Depth; z++)
			{
				for (int x = 0; x < level.Width; x++)
				{
					pixels[z * level.Width + x] = new Color32(sides[x, z].a, sides[x, z].b, footprints[x, z], 0);
				}
			}

			_rake.SetPixels32(pixels);
			_rake.Apply();
			BuildCircles(level);

			_renderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetTexture(RakeId, _rake);
			_propertyBlock.SetVectorArray(CirclesId, _circles);
			_propertyBlock.SetFloat(CircleCountId, _circleCount);
			_renderer.SetPropertyBlock(_propertyBlock);
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

			// The border, like any tile without grass, is bare dirt
			Color32[] dirt = new Color32[width * depth];
			Array.Fill(dirt, (Color32)Color.white);
			_cells.SetPixels32(dirt);

			foreach (GridVector position in level.Positions)
			{
				if (level.GetTile(position) is not IReadonlyGrassTile grass)
				{
					continue;
				}

				SetCell(position, GrassColour(grass.GrassHeight.Value), false);

				GridVector cell = position;
				UndoableAction<int, int> onHeightChanged = (_, height, _) => SetCell(cell, GrassColour(height), true);
				grass.GrassHeight.ValueChangedFromTo += onHeightChanged;
				_unsubscribes.Add(() => grass.GrassHeight.ValueChangedFromTo -= onHeightChanged);
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

		private Color GrassColour(int height)
		{
			Color colour = grassColours[Mathf.Clamp(height, 0, grassColours.Length - 1)];
			colour.a = height <= 0 ? 1 : 0;
			return colour;
		}

		private void Unsubscribe()
		{
			foreach (Action unsubscribe in _unsubscribes)
			{
				unsubscribe();
			}

			_unsubscribes.Clear();
		}

		// Round ground the rake goes round that isn't a tile's: the landing markers' mushrooms. Each is its centre (xy, in
		// tiles) and radius (z)
		private void BuildCircles(Level level)
		{
			_circleCount = 0;
			foreach (GridVector position in level.Positions)
			{
				if (level.GetTile(position) is not IReadonlySpringTile spring)
				{
					continue;
				}

				GridVector landing = position + spring.Offset;
				foreach (Vector2 stem in landingMarkerStems)
				{
					if (_circleCount == MaxCircles)
					{
						Debug.LogWarning($"Only {MaxCircles} landing marker mushrooms can be raked round", this);
						return;
					}

					_circles[_circleCount++] = new Vector4(landing.x + 0.5f + stem.x, landing.y + 0.5f + stem.y,
						landingMarkerFootprint, 0);
				}
			}
		}

		private float Footprint(TileKind kind)
		{
			return kind switch
			{
				TileKind.Stone => stoneFootprint,
				TileKind.Spring => springFootprint,
				_ => 0,
			};
		}

		internal static bool IsSolid(TileKind kind)
		{
			return kind is TileKind.Grass or TileKind.Stone or TileKind.Spring;
		}

		#endregion
	}
}
