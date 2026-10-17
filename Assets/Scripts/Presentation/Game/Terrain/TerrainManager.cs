using Game.Core;
using Game.Levels;
using Lawnmower.Model;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
	/// <summary>
	/// Builds one smooth terrain mesh under each level from an SDF of its solid tiles, in place of a block per tile.
	/// The mesh sits in the level container's space, where tile (x, z) covers [x, x + 1] x [z, z + 1]. Its top is raked
	/// dirt throughout, along a <see cref="RakePath"/> through the solid tiles.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	internal class TerrainManager : MonoBehaviour
	{
		[SerializeField] private TerrainSettings settings;
		[SerializeField] private TerrainEdging edging;

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelStartedEventChannel;

		private static readonly int RakeId = Shader.PropertyToID("_TerrainRake");

		private Level _level;
		private Mesh _mesh;
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
			Destroy(_mesh);
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
			Build(level);
			BuildRake(level);
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

			bool[,] rakeable = new bool[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				rakeable[position.x, position.y] = IsSolid(level.GetTile(position).Kind);
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

			_renderer.GetPropertyBlock(_propertyBlock);
			_propertyBlock.SetTexture(RakeId, _rake);
			_renderer.SetPropertyBlock(_propertyBlock);
		}

		internal static bool IsSolid(TileKind kind)
		{
			return kind is TileKind.Grass or TileKind.Stone or TileKind.Spring;
		}

		#endregion
	}
}
