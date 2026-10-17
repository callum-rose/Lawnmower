using System;
using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Tiles;
using Lawnmower.Model;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
	/// <summary>
	/// The sea every level sits in: one water surface reaching far past the screen on every side, a seabed under it and a
	/// ripple simulation over the level whose walls follow the terrain's shoreline. Water tiles, empty tiles and the
	/// space around the level are all the same sea. The seabed sits just under the terrain next to land and shelves off
	/// into deep water away from it. Bumping into a water tile splashes it.
	/// Sits in the terrain's resting space (tile (x, z) covers [x, x + 1] x [z, z + 1]). The surface stays still while
	/// the terrain rises through it in the level intro; the floor should be under the terrain so it rises with it.
	/// </summary>
	internal class PondManager : MonoBehaviour
	{
		[SerializeField] private TerrainSettings settings;
		[Tooltip("The water: on the Water layer, with the ripple simulation and planar reflection")]
		[SerializeField] private MeshFilter surface;
		[SerializeField] private WaterRippleSimulation rippleSimulation;
		[Tooltip("The seabed; under the terrain so it rises with it")]
		[SerializeField] private MeshFilter floor;

		[Tooltip("How far the water surface sits below the terrain top")]
		[SerializeField] private float surfaceDepth = 0.08f;
		[Tooltip("How far the sea reaches past the level on every side, in tiles")]
		[SerializeField] private float seaExtent = 100;
		[Tooltip("How far the ripple simulation reaches past the level on every side, in tiles")]
		[SerializeField] private float rippleMargin = 2.5f;

		[Header("Floor")]
		[Tooltip("How far the floor sits below the terrain top next to land")]
		[SerializeField] private float floorDepth = 0.95f;
		[Tooltip("How far the floor sits below the terrain top out at sea")]
		[SerializeField] private float deepFloorDepth = 3;
		[Tooltip("Distance from land, in tiles, where the floor starts shelving down and where it reaches the deep")]
		[SerializeField] private Vector2 shelf = new Vector2(1, 4);
		[Tooltip("Spacing of the floor's vertices around the level, in tiles")]
		[SerializeField] private float floorSpacing = 0.25f;

		[Header("Bump Splash")]
		[Tooltip("How far into the water tile from the side the mower bumped, in tiles")]
		[SerializeField] private float splashInset = 0.3f;
		[SerializeField] private float splashRadius = 0.08f;
		[SerializeField] private float splashStrength = 1.5f;

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelStartedEventChannel;

		private readonly List<Action> _unsubscribes = new List<Action>();

		private Level _level;
		private Mesh _surfaceMesh;
		private Mesh _floorMesh;
		private Texture2D _mask;

		/// <summary>The water surface's world height.</summary>
		public float SurfaceHeight => transform.TransformPoint(new Vector3(0, settings.TopHeight - surfaceDepth, 0)).y;

		#region Unity

		private void Awake()
		{
			_surfaceMesh = new Mesh { name = "Sea" };
			_floorMesh = new Mesh { name = "Seabed" };
			surface.sharedMesh = _surfaceMesh;
			floor.sharedMesh = _floorMesh;
			BuildSurfaceMesh();
			SetShowing(false);

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

			Destroy(_surfaceMesh);
			Destroy(_floorMesh);
			if (_mask)
			{
				Destroy(_mask);
			}
		}

		#endregion

		#region Events

		private void OnLevelStarted(Level level)
		{
			_level = level;
			Build(level);
			SubscribeToBumps(level);
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
				solid[position.x, position.y] = TerrainManager.IsSolid(level.GetTile(position).Kind);
			}

			TerrainFootprint footprint = TerrainFootprint.Create(solid, settings.ConvexCornerRadius,
				settings.ConcaveCornerRadius, Allocator.TempJob);

			try
			{
				Vector2 centre = new Vector2(level.Width, level.Depth) / 2;
				float surfaceHeight = settings.TopHeight - surfaceDepth;
				surface.transform.localPosition = new Vector3(centre.x, surfaceHeight, centre.y);

				BuildFloorMesh(footprint, level.Width, level.Depth, centre);
				floor.transform.localPosition = Vector3.zero;

				SetShowing(true);
				Rect rippleBounds = new Rect(-rippleMargin, -rippleMargin, level.Width + rippleMargin * 2,
					level.Depth + rippleMargin * 2);
				rippleSimulation.Size = rippleBounds.size * LevelDimensions.TileSize;
				BuildMask(footprint, rippleBounds, surfaceHeight);
				rippleSimulation.Mask = _mask;
				rippleSimulation.Clear();
			}
			finally
			{
				footprint.Dispose();
			}
		}

		private void SubscribeToBumps(Level level)
		{
			Unsubscribe();

			foreach (GridVector position in level.Positions)
			{
				Tile tile = level.GetTile(position);
				if (tile.Kind != TileKind.Water)
				{
					continue;
				}

				Vector3 centre = new Vector3(position.x + 0.5f, 0, position.y + 0.5f);
				TraverseEvent onBumped = (direction, _) =>
					Splash(centre - new Vector3(direction.x, 0, direction.y) * (0.5f - splashInset));
				tile.BumpedInto += onBumped;
				_unsubscribes.Add(() => tile.BumpedInto -= onBumped);
			}
		}

		private void Splash(Vector3 localPosition)
		{
			if (rippleSimulation.isActiveAndEnabled)
			{
				rippleSimulation.Splash(transform.TransformPoint(localPosition), splashRadius, splashStrength);
			}
		}

		private void Unsubscribe()
		{
			foreach (Action unsubscribe in _unsubscribes)
			{
				unsubscribe();
			}

			_unsubscribes.Clear();
		}

		private void SetShowing(bool showing)
		{
			surface.gameObject.SetActive(showing);
			floor.gameObject.SetActive(showing);
		}

		/// <summary>One quad, centred on the surface's transform, reaching <see cref="seaExtent"/> past any level.</summary>
		private void BuildSurfaceMesh()
		{
			float extent = seaExtent;
			_surfaceMesh.SetVertices(new[]
			{
				new Vector3(-extent, 0, -extent), new Vector3(-extent, 0, extent),
				new Vector3(extent, 0, extent), new Vector3(extent, 0, -extent),
			});
			_surfaceMesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
			_surfaceMesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
			_surfaceMesh.RecalculateBounds();
		}

		/// <summary>
		/// A grid in the floor's parent space: fine around the level, where the floor shelves from just under the terrain
		/// down to the deep, plus one row and column out to <see cref="seaExtent"/> on each side.
		/// </summary>
		private void BuildFloorMesh(TerrainFootprint footprint, int width, int depth, Vector2 centre)
		{
			float margin = shelf.y + floorSpacing;
			using NativeArray<float> xs = GetFloorLines(-margin, width + margin, centre.x);
			using NativeArray<float> zs = GetFloorLines(-margin, depth + margin, centre.y);
			using NativeArray<float3> vertices = new NativeArray<float3>(xs.Length * zs.Length, Allocator.TempJob);

			new FloorJob
			{
				Footprint = footprint,
				Xs = xs,
				Zs = zs,
				Top = settings.TopHeight,
				Depth = new float2(floorDepth, deepFloorDepth),
				Shelf = shelf,
				Vertices = vertices,
			}.Schedule(vertices.Length, 64).Complete();

			int[] triangles = new int[(xs.Length - 1) * (zs.Length - 1) * 6];
			int t = 0;
			for (int z = 0; z < zs.Length - 1; z++)
			{
				for (int x = 0; x < xs.Length - 1; x++)
				{
					int a = x + z * xs.Length;
					int b = a + xs.Length;
					triangles[t++] = a;
					triangles[t++] = b;
					triangles[t++] = b + 1;
					triangles[t++] = a;
					triangles[t++] = b + 1;
					triangles[t++] = a + 1;
				}
			}

			_floorMesh.Clear();
			_floorMesh.SetVertices(vertices);
			_floorMesh.SetTriangles(triangles, 0);
			_floorMesh.RecalculateNormals();
			_floorMesh.RecalculateBounds();
		}

		/// <summary>Evenly spaced lines from <paramref name="min"/> to <paramref name="max"/>, plus the sea's edges.</summary>
		private NativeArray<float> GetFloorLines(float min, float max, float centre)
		{
			int count = Mathf.CeilToInt((max - min) / floorSpacing);
			NativeArray<float> lines = new NativeArray<float>(count + 3, Allocator.TempJob);
			lines[0] = centre - seaExtent;
			for (int i = 0; i <= count; i++)
			{
				lines[i + 1] = Mathf.Lerp(min, max, (float)i / count);
			}

			lines[count + 2] = centre + seaExtent;
			return lines;
		}

		/// <summary>
		/// Marks which ripple simulation texels are open water: outside the terrain at the water's surface, so ripples
		/// bounce off the real shoreline rather than the tile grid.
		/// </summary>
		private void BuildMask(TerrainFootprint footprint, Rect bounds, float surfaceHeight)
		{
			Vector2Int resolution = rippleSimulation.Resolution;
			if (_mask == null || _mask.width != resolution.x || _mask.height != resolution.y)
			{
				if (_mask)
				{
					Destroy(_mask);
				}

				_mask = new Texture2D(resolution.x, resolution.y, TextureFormat.R8, false, true)
				{
					name = "SeaMask",
					filterMode = FilterMode.Point,
					wrapMode = TextureWrapMode.Clamp,
				};
			}

			float margin = settings.Margin;
			using NativeArray<byte> open = new NativeArray<byte>(resolution.x * resolution.y, Allocator.TempJob);

			new MaskJob
			{
				Footprint = footprint,
				Shape = settings.Shape,
				Bounds = new float4(bounds.xMin, bounds.yMin, bounds.width, bounds.height),
				TerrainBounds = new float4(-margin, -margin, footprint.Width + margin, footprint.Depth + margin),
				Resolution = new int2(resolution.x, resolution.y),
				SurfaceHeight = surfaceHeight,
				Open = open,
			}.Schedule(open.Length, 256).Complete();

			_mask.SetPixelData(open, 0);
			_mask.Apply();
		}

		#endregion

		#region Jobs

		[BurstCompile]
		private struct FloorJob : IJobParallelFor
		{
			[ReadOnly] public TerrainFootprint Footprint;
			[ReadOnly] public NativeArray<float> Xs;
			[ReadOnly] public NativeArray<float> Zs;
			public float Top;
			/// <summary>Below the top: next to land, then out at sea.</summary>
			public float2 Depth;
			public float2 Shelf;
			[WriteOnly] public NativeArray<float3> Vertices;

			public void Execute(int index)
			{
				float2 p = new float2(Xs[index % Xs.Length], Zs[index / Xs.Length]);
				float deep = math.smoothstep(Shelf.x, Shelf.y, Footprint.Distance(p));
				Vertices[index] = new float3(p.x, Top - math.lerp(Depth.x, Depth.y, deep), p.y);
			}
		}

		/// <summary>Open water is outside the terrain at the water's surface; far from the level it always is.</summary>
		[BurstCompile]
		private struct MaskJob : IJobParallelFor
		{
			[ReadOnly] public TerrainFootprint Footprint;
			public TerrainShape Shape;
			/// <summary>Min x, min z, width, depth.</summary>
			public float4 Bounds;
			/// <summary>Min x, min z, max x, max z.</summary>
			public float4 TerrainBounds;
			public int2 Resolution;
			public float SurfaceHeight;
			[WriteOnly] public NativeArray<byte> Open;

			public void Execute(int index)
			{
				int2 texel = new int2(index % Resolution.x, index / Resolution.x);
				float2 p = Bounds.xy + ((float2)texel + 0.5f) / Resolution * Bounds.zw;

				bool isOpen = math.any(p < TerrainBounds.xy) || math.any(p > TerrainBounds.zw) ||
				              Shape.Evaluate(new float3(p.x, SurfaceHeight, p.y), Footprint.Distance(p)) > 0;
				Open[index] = (byte)(isOpen ? 255 : 0);
			}
		}

		#endregion
	}
}
