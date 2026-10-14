using System.Collections.Generic;
using Game.Core;
using Game.Levels;
using Game.Tiles;
using Lawnmower.Model;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Terrain
{
	/// <summary>
	/// Fills the holes the terrain leaves at a level's water tiles with one pond: a water surface, a floor under it and a
	/// ripple simulation whose walls follow the terrain's shoreline. Wood tiles next to water are bridges, so the pond
	/// runs under them. The surface and floor reach half a tile under the solid tiles around the pond, because the
	/// terrain's banks are inset from the tile grid above its pivot height.
	/// Sits under the terrain in the same space (tile (x, z) covers [x, x + 1] x [z, z + 1]), so it rises with it.
	/// </summary>
	internal class PondManager : MonoBehaviour
	{
		[SerializeField] private TerrainSettings settings;
		[Tooltip("The water: on the Water layer, with the ripple simulation and planar reflection")]
		[SerializeField] private MeshFilter surface;
		[SerializeField] private WaterRippleSimulation rippleSimulation;
		[SerializeField] private MeshFilter floor;

		[Tooltip("How far the water surface sits below the terrain top")]
		[SerializeField] private float surfaceDepth = 0.08f;
		[Tooltip("How far the floor sits below the terrain top")]
		[SerializeField] private float floorDepth = 0.95f;

		[Header("Event Channels")]
		[SerializeField] private LevelDataEventChannel levelStartedEventChannel;

		private Level _level;
		private Mesh _mesh;
		private Texture2D _mask;

		#region Unity

		private void Awake()
		{
			_mesh = new Mesh { name = "Pond" };
			surface.sharedMesh = _mesh;
			floor.sharedMesh = _mesh;
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

			Destroy(_mesh);
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
			bool[,] pond = GetPondTiles(level);
			bool[,] solid = new bool[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				solid[position.x, position.y] = TerrainManager.IsSolid(level.GetTile(position).Kind);
			}

			bool[,] cells = GetPondCells(pond, solid, out RectInt cellBounds);
			if (cellBounds.width == 0)
			{
				SetShowing(false);
				return;
			}

			// In tiles; a cell is half a tile
			Rect bounds = new Rect(cellBounds.x * 0.5f, cellBounds.y * 0.5f, cellBounds.width * 0.5f,
				cellBounds.height * 0.5f);
			float surfaceHeight = settings.TopHeight - surfaceDepth;

			BuildMesh(cells, bounds.center);
			surface.transform.localPosition = new Vector3(bounds.center.x, surfaceHeight, bounds.center.y);
			floor.transform.localPosition = new Vector3(bounds.center.x, settings.TopHeight - floorDepth, bounds.center.y);

			SetShowing(true);
			rippleSimulation.Size = bounds.size * LevelDimensions.TileSize;
			BuildMask(cells, solid, bounds, surfaceHeight);
			rippleSimulation.Mask = _mask;
			rippleSimulation.Clear();
		}

		private void SetShowing(bool showing)
		{
			surface.gameObject.SetActive(showing);
			floor.gameObject.SetActive(showing);
		}

		private static bool[,] GetPondTiles(Level level)
		{
			bool[,] pond = new bool[level.Width, level.Depth];
			foreach (GridVector position in level.Positions)
			{
				pond[position.x, position.y] = level.GetTile(position).Kind == TileKind.Water;
			}

			foreach (GridVector position in level.Positions)
			{
				if (level.GetTile(position).Kind == TileKind.Wood && IsNextToWater(level, position))
				{
					pond[position.x, position.y] = true;
				}
			}

			return pond;
		}

		private static bool IsNextToWater(Level level, GridVector position)
		{
			foreach (GridVector offset in new[] { new GridVector(1, 0), new GridVector(-1, 0), new GridVector(0, 1), new GridVector(0, -1) })
			{
				if (level.GetTile(position + offset)?.Kind == TileKind.Water)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The half-tile cells the pond covers: all of each pond tile, plus each quarter of a solid tile that borders the
		/// pond (along a side or at a corner), where the terrain's inset bank would otherwise leave a gap.
		/// </summary>
		private static bool[,] GetPondCells(bool[,] pond, bool[,] solid, out RectInt bounds)
		{
			int width = pond.GetLength(0);
			int depth = pond.GetLength(1);
			bool[,] cells = new bool[width * 2, depth * 2];
			int2 min = new int2(int.MaxValue);
			int2 max = new int2(int.MinValue);

			bool IsPond(int x, int z) => x >= 0 && z >= 0 && x < width && z < depth && pond[x, z];

			for (int cz = 0; cz < depth * 2; cz++)
			{
				for (int cx = 0; cx < width * 2; cx++)
				{
					int x = cx / 2;
					int z = cz / 2;
					int dx = cx % 2 == 0 ? -1 : 1;
					int dz = cz % 2 == 0 ? -1 : 1;

					bool covered = pond[x, z] ||
					               (solid[x, z] && (IsPond(x + dx, z) || IsPond(x, z + dz) || IsPond(x + dx, z + dz)));
					if (!covered)
					{
						continue;
					}

					cells[cx, cz] = true;
					min = math.min(min, new int2(cx, cz));
					max = math.max(max, new int2(cx, cz));
				}
			}

			bounds = min.x > max.x ? new RectInt() : new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
			return cells;
		}

		/// <summary>Flat quads over the covered cells, centred on <paramref name="centre"/>. Shared by the surface and floor.</summary>
		private void BuildMesh(bool[,] cells, Vector2 centre)
		{
			int width = cells.GetLength(0);
			int depth = cells.GetLength(1);
			int[,] vertexIndices = new int[width + 1, depth + 1];
			List<Vector3> vertices = new List<Vector3>();
			List<int> triangles = new List<int>();

			int Vertex(int vx, int vz)
			{
				if (vertexIndices[vx, vz] == 0)
				{
					vertices.Add(new Vector3(vx * 0.5f - centre.x, 0, vz * 0.5f - centre.y));
					vertexIndices[vx, vz] = vertices.Count;
				}

				return vertexIndices[vx, vz] - 1;
			}

			for (int cz = 0; cz < depth; cz++)
			{
				for (int cx = 0; cx < width; cx++)
				{
					if (!cells[cx, cz])
					{
						continue;
					}

					int a = Vertex(cx, cz);
					int b = Vertex(cx, cz + 1);
					int c = Vertex(cx + 1, cz + 1);
					int d = Vertex(cx + 1, cz);
					triangles.AddRange(new[] { a, b, c, a, c, d });
				}
			}

			Vector3[] normals = new Vector3[vertices.Count];
			System.Array.Fill(normals, Vector3.up);

			_mesh.Clear();
			_mesh.SetVertices(vertices);
			_mesh.SetNormals(normals);
			_mesh.SetTriangles(triangles, 0);
			_mesh.RecalculateBounds();
		}

		/// <summary>
		/// Marks which ripple simulation texels are open water: inside the pond's cells and outside the terrain at the
		/// water's surface, so ripples bounce off the real shoreline rather than the tile grid.
		/// </summary>
		private void BuildMask(bool[,] cells, bool[,] solid, Rect bounds, float surfaceHeight)
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
					name = "PondMask",
					filterMode = FilterMode.Point,
					wrapMode = TextureWrapMode.Clamp,
				};
			}

			TerrainShape shape = settings.Shape;
			TerrainFootprint footprint = TerrainFootprint.Create(solid, settings.ConvexCornerRadius,
				settings.ConcaveCornerRadius, Allocator.Temp);

			try
			{
				byte[] open = new byte[resolution.x * resolution.y];

				for (int v = 0; v < resolution.y; v++)
				{
					for (int u = 0; u < resolution.x; u++)
					{
						float2 p = new float2(bounds.xMin + (u + 0.5f) / resolution.x * bounds.width,
							bounds.yMin + (v + 0.5f) / resolution.y * bounds.height);

						bool isOpen = cells[(int)(p.x * 2), (int)(p.y * 2)] &&
						              shape.Evaluate(new float3(p.x, surfaceHeight, p.y), footprint.Distance(p)) > 0;
						open[u + v * resolution.x] = (byte)(isOpen ? 255 : 0);
					}
				}

				_mask.SetPixelData(open, 0);
				_mask.Apply();
			}
			finally
			{
				footprint.Dispose();
			}
		}

		#endregion
	}
}
