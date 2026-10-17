using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Game.Terrain
{
	/// <summary>
	/// Rings the terrain's shoreline with rocks, combined into one mesh with a submesh per material. Rocks are laid end to
	/// end along a locus offset outwards from each straight side and corner arc of the <see cref="TerrainFootprint"/>,
	/// out on the bare shore past the tiles and sunk into the rounded top edge, each turned to follow the shore in 5°
	/// steps. Round rocks go everywhere; long slabs are mixed in only on long straight runs, never round a corner or at
	/// either end of a run. Pebbles fill some of the joints between rocks, on the water side. Which rock goes where is hashed from its position, so a level always looks the same.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	internal class TerrainEdging : MonoBehaviour
	{
		private const float RotationStep = 5;

		[Tooltip("The round rocks model (Assets/Models/Terrain/shore_rocks.fbx): one readable mesh per rock")]
		[FormerlySerializedAs("cobbles")]
		[SerializeField] private GameObject rocks;

		[Tooltip("The long slabs model (Assets/Models/Terrain/shore_slabs.fbx), for long straight runs only. Optional")]
		[SerializeField] private GameObject slabs;

		[Tooltip("Space left between neighbouring rocks along the shoreline, in tiles; negative overlaps them")]
		[SerializeField] private float gap = -0.05f;

		[Tooltip("The shortest straight run, in tiles, that slabs are mixed into")]
		[SerializeField, Min(0)] private float slabMinRun = 1.8f;

		[Tooltip("Chance of each rock on a long straight run being a slab, its two ends aside")]
		[SerializeField, Range(0, 1)] private float slabChance = 0.5f;

		[Tooltip("The pebbles model (Assets/Models/Terrain/shore_pebbles.fbx), for the joints between rocks. Optional")]
		[SerializeField] private GameObject pebbles;

		[Tooltip("Chance of a pebble in each joint between two rocks, and of a second beside it")]
		[SerializeField, Range(0, 1)] private float pebbleChance = 0.85f;

		[Tooltip("How far out past the rocks' centre line joint pebbles sit, towards the water, in tiles")]
		[SerializeField] private Vector2 pebbleOut = new Vector2(0.1f, 0.17f);

		[Tooltip("How far each rock's centre sits inside the shore's outer edge, in tiles")]
		[SerializeField] private float inset = 0.14f;

		[Tooltip("How far each rock's base sits below the terrain top")]
		[SerializeField] private float sink = 0.1f;

		[Tooltip("Random turn either side of the shoreline, in whole 5° steps")]
		[SerializeField, Range(0, 4)] private int turnSteps = 2;

		[SerializeField] private uint seed = 1;

		private struct Cobble
		{
			public Vector3[] Positions;
			public Vector3[] Normals;
			public Color32[] Colours;
			/// <summary>The baked shade: AO in U, the moss mask in V.</summary>
			public Vector2[] Shade;
			/// <summary>Triangles per submesh, as indices into <see cref="Materials"/>' combined submeshes.</summary>
			public int[][] Triangles;
			public int[] Materials;
			/// <summary>The rock's extent along its X axis, which is laid along the shore.</summary>
			public float Length;
		}

		private Cobble[] _rocks;
		private Cobble[] _slabs;
		private Cobble[] _pebbles;
		private float _shore;
		private float _averageRockLength;
		private Material[] _materials;
		private Mesh _mesh;

		private readonly List<Vector3> _positions = new List<Vector3>();
		private readonly List<Vector3> _normals = new List<Vector3>();
		private readonly List<Color32> _colours = new List<Color32>();
		private readonly List<Vector2> _shade = new List<Vector2>();
		private List<int>[] _triangles;
		private readonly List<Cobble> _run = new List<Cobble>();
		private readonly List<float4> _runs = new List<float4>();
		private readonly Dictionary<int2, int> _segmentStarts = new Dictionary<int2, int>();

		#region Unity

		private void Awake()
		{
			_mesh = new Mesh { name = "TerrainEdging", indexFormat = IndexFormat.UInt32 };
			GetComponent<MeshFilter>().sharedMesh = _mesh;
			LoadModels();
			GetComponent<MeshRenderer>().sharedMaterials = _materials;
		}

		private void OnDestroy()
		{
			Destroy(_mesh);
		}

		#endregion

		#region API

		/// <param name="shore">How far the terrain's top runs past the tiles' outline.</param>
		public void Build(TerrainFootprint footprint, float topHeight, float shore)
		{
			_positions.Clear();
			_normals.Clear();
			_colours.Clear();
			_shade.Clear();
			foreach (List<int> triangles in _triangles)
			{
				triangles.Clear();
			}

			float y = topHeight - sink;
			float offset = shore - inset;
			_shore = shore;

			MergeRuns(footprint.Segments);
			foreach (float4 run in _runs)
			{
				AddRun(footprint, run.xy, run.zw, offset, y);
			}

			foreach (FootprintArc arc in footprint.Arcs)
			{
				// Offsetting outwards grows a convex arc (centred inside) and shrinks a concave one, down to its corner
				bool convex = footprint.IsInside(arc.Centre);
				float radius = math.max(0, arc.Radius + (convex ? offset : -offset));
				int count = Mathf.Max(1, Mathf.RoundToInt(radius * math.PI * 0.5f / math.max(_averageRockLength + gap, 0.05f)));

				for (int i = 0; i < count; i++)
				{
					float angle = (i + 0.5f) / count * math.PI * 0.5f;
					float2 direction = new float2(-arc.Facing.x * math.cos(angle), -arc.Facing.y * math.sin(angle));
					float2 point = arc.Centre + direction * radius;
					Unity.Mathematics.Random random = Random(point, 0);
					AddCobble(footprint, _rocks[random.NextInt(_rocks.Length)], point, new float2(-direction.y, direction.x), 0,
						y, convex ? direction : -direction);
				}

				// The joints between the corner's rocks, and at either end with the sides' rocks
				for (int i = 0; i <= count; i++)
				{
					float angle = (float)i / count * math.PI * 0.5f;
					float2 direction = new float2(-arc.Facing.x * math.cos(angle), -arc.Facing.y * math.sin(angle));
					Unity.Mathematics.Random random = Random(arc.Centre + direction, 4);
					AddJointPebbles(footprint, arc.Centre + direction * arc.Radius, new float2(-direction.y, direction.x),
						convex ? direction : -direction, offset, y, ref random);
				}
			}

			_mesh.Clear();
			_mesh.SetVertices(_positions);
			_mesh.SetNormals(_normals);
			_mesh.SetColors(_colours);
			_mesh.SetUVs(0, _shade);
			_mesh.subMeshCount = _triangles.Length;
			for (int i = 0; i < _triangles.Length; i++)
			{
				_mesh.SetTriangles(_triangles[i], i);
			}

			_mesh.RecalculateBounds();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Joins the footprint's per-tile side segments into straight runs: a segment carries on from another when it
		/// starts where that one ends, heading the same way.
		/// </summary>
		private void MergeRuns(NativeArray<float4> segments)
		{
			_runs.Clear();
			_segmentStarts.Clear();
			for (int i = 0; i < segments.Length; i++)
			{
				_segmentStarts[PointKey(segments[i].xy)] = i;
			}

			for (int i = 0; i < segments.Length; i++)
			{
				float4 segment = segments[i];
				if (Continues(segments, segment))
				{
					continue;
				}

				float2 end = segment.zw;
				while (_segmentStarts.TryGetValue(PointKey(end), out int next) && Continues(segments, segments[next]))
				{
					end = segments[next].zw;
				}

				_runs.Add(new float4(segment.xy, end));
			}
		}

		/// <summary>Whether <paramref name="segment"/> carries straight on from another segment ending at its start.</summary>
		private static bool Continues(NativeArray<float4> segments, float4 segment)
		{
			float2 direction = math.normalize(segment.zw - segment.xy);
			for (int i = 0; i < segments.Length; i++)
			{
				float4 other = segments[i];
				if (math.distancesq(other.zw, segment.xy) < 1e-6f &&
				    math.dot(math.normalize(other.zw - other.xy), direction) > 0.999f)
				{
					return true;
				}
			}

			return false;
		}

		private static int2 PointKey(float2 point)
		{
			return (int2)math.round(point * 1000);
		}

		/// <summary>
		/// Picks rocks to fill the straight run from <paramref name="a"/> to <paramref name="b"/> end to end, then stretches
		/// or squeezes the gaps between them so the run fits exactly.
		/// </summary>
		private void AddRun(TerrainFootprint footprint, float2 a, float2 b, float offset, float y)
		{
			float length = math.distance(a, b);
			if (length < 1e-4f)
			{
				return;
			}

			Unity.Mathematics.Random random = Random(a, 1);
			bool useSlabs = _slabs.Length > 0 && length >= slabMinRun;

			_run.Clear();
			float total = 0;
			while (total < length)
			{
				bool slab = useSlabs && _run.Count > 0 && random.NextFloat() < slabChance;
				Cobble cobble = slab ? _slabs[random.NextInt(_slabs.Length)] : _rocks[random.NextInt(_rocks.Length)];
				_run.Add(cobble);
				total += cobble.Length + gap;
			}

			// Drop the last rock if the run comes closer to fitting without it
			Cobble last = _run[_run.Count - 1];
			if (_run.Count > 1 && total - length > (last.Length + gap) * 0.5f)
			{
				_run.RemoveAt(_run.Count - 1);
				total -= last.Length + gap;
			}

			// Corners stay round: a run never ends on a slab
			if (useSlabs && _run.Count > 1)
			{
				last = _run[_run.Count - 1];
				Cobble rock = _rocks[random.NextInt(_rocks.Length)];
				_run[_run.Count - 1] = rock;
				total += rock.Length - last.Length;
			}

			float2 tangent = (b - a) / length;
			float2 outward = Outward(footprint, math.lerp(a, b, 0.5f), tangent);
			float scale = length / total;
			float along = 0;
			Unity.Mathematics.Random pebbleRandom = Random(a, 3);
			for (int i = 0; i < _run.Count; i++)
			{
				float slot = (_run[i].Length + gap) * scale;
				AddCobble(footprint, _run[i], a + tangent * (along + slot * 0.5f), tangent, offset, y, outward);
				along += slot;

				if (i < _run.Count - 1)
				{
					AddJointPebbles(footprint, a + tangent * along, tangent, outward, offset, y, ref pebbleRandom);
				}
			}
		}

		/// <summary>Maybe one pebble, maybe two side by side, in the joint at <paramref name="point"/> on the tiles' outline.</summary>
		private void AddJointPebbles(TerrainFootprint footprint, float2 point, float2 tangent, float2 outward, float offset,
			float y, ref Unity.Mathematics.Random random)
		{
			if (_pebbles.Length == 0)
			{
				return;
			}

			for (int i = 0; i < 2 && random.NextFloat() < pebbleChance; i++)
			{
				float2 at = point + tangent * random.NextFloat(-0.05f, 0.05f);
				AddPebble(footprint, at, outward, offset + random.NextFloat(pebbleOut.x, pebbleOut.y), y, ref random);
			}
		}

		/// <param name="point">A point on the tiles' outline.</param>
		/// <param name="offset">How far out from <paramref name="point"/> the pebble lands.</param>
		private void AddPebble(TerrainFootprint footprint, float2 point, float2 outward, float offset, float y,
			ref Unity.Mathematics.Random random)
		{
			float angle = random.NextFloat(math.PI * 2);
			// Out past the flat top the shore rounds down towards the water, so settle the pebble down with it
			float drop = math.max(0, offset - _shore) * 0.5f;
			AddCobble(footprint, _pebbles[random.NextInt(_pebbles.Length)], point, new float2(math.cos(angle), math.sin(angle)),
				offset, y - drop, outward);
		}

		/// <summary>The way out from the land at <paramref name="point"/>, on a side running along <paramref name="tangent"/>.</summary>
		private static float2 Outward(TerrainFootprint footprint, float2 point, float2 tangent)
		{
			// The outline's winding isn't known here, so find which side of it is solid
			float2 normal = new float2(tangent.y, -tangent.x);
			return footprint.IsInside(point + normal * 0.02f) ? -normal : normal;
		}

		private Unity.Mathematics.Random Random(float2 point, int salt)
		{
			uint hash = math.hash(new int4((int2)math.round(point * 1000), (int)seed, salt));
			return new Unity.Mathematics.Random(hash | 1);
		}

		/// <param name="offset">How far out from <paramref name="point"/> the rock sits.</param>
		/// <param name="outward">The way out from the land, if known.</param>
		private void AddCobble(TerrainFootprint footprint, Cobble cobble, float2 point, float2 tangent, float offset,
			float y, float2? outward = null)
		{
			float2 normal = outward ?? Outward(footprint, point, tangent);
			float2 centre = point + normal * offset;

			Unity.Mathematics.Random random = Random(centre, 2);

			// Each rock's length runs along its X axis; lay that along the shore
			float yaw = math.degrees(math.atan2(-tangent.y, tangent.x));
			yaw = math.round(yaw / RotationStep) * RotationStep + random.NextInt(-turnSteps, turnSteps + 1) * RotationStep;

			Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(centre.x, y, centre.y), Quaternion.Euler(0, yaw, 0), Vector3.one);
			int first = _positions.Count;

			for (int i = 0; i < cobble.Positions.Length; i++)
			{
				_positions.Add(matrix.MultiplyPoint3x4(cobble.Positions[i]));
				_normals.Add(matrix.MultiplyVector(cobble.Normals[i]));
				_colours.Add(cobble.Colours[i]);
				_shade.Add(cobble.Shade.Length > 0 ? cobble.Shade[i] : new Vector2(1, 0));
			}

			for (int sub = 0; sub < cobble.Triangles.Length; sub++)
			{
				List<int> triangles = _triangles[cobble.Materials[sub]];
				foreach (int index in cobble.Triangles[sub])
				{
					triangles.Add(first + index);
				}
			}
		}

		private void LoadModels()
		{
			List<Material> materials = new List<Material>();
			_rocks = LoadCobbles(rocks, materials);
			_slabs = slabs ? LoadCobbles(slabs, materials) : new Cobble[0];
			_pebbles = pebbles ? LoadCobbles(pebbles, materials) : new Cobble[0];
			_materials = materials.ToArray();
			_triangles = new List<int>[_materials.Length];
			for (int i = 0; i < _triangles.Length; i++)
			{
				_triangles[i] = new List<int>();
			}

			_averageRockLength = 0;
			foreach (Cobble rock in _rocks)
			{
				_averageRockLength += rock.Length / _rocks.Length;
			}
		}

		/// <summary>Flattens each rock in the model into the model root's space, and gathers their materials.</summary>
		private static Cobble[] LoadCobbles(GameObject model, List<Material> materials)
		{
			List<Cobble> cobbleList = new List<Cobble>();
			Matrix4x4 toRoot = model.transform.worldToLocalMatrix;

			foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
			{
				Mesh mesh = filter.sharedMesh;
				Material[] meshMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
				Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

				Vector3[] positions = mesh.vertices;
				Vector3[] normals = mesh.normals;
				float minX = float.MaxValue, maxX = float.MinValue;
				for (int i = 0; i < positions.Length; i++)
				{
					positions[i] = matrix.MultiplyPoint3x4(positions[i]);
					normals[i] = matrix.MultiplyVector(normals[i]).normalized;
					minX = Mathf.Min(minX, positions[i].x);
					maxX = Mathf.Max(maxX, positions[i].x);
				}

				int[][] triangles = new int[mesh.subMeshCount][];
				int[] materialIndices = new int[mesh.subMeshCount];
				for (int sub = 0; sub < mesh.subMeshCount; sub++)
				{
					triangles[sub] = mesh.GetTriangles(sub);

					Material material = meshMaterials[Mathf.Min(sub, meshMaterials.Length - 1)];
					materialIndices[sub] = materials.IndexOf(material);
					if (materialIndices[sub] < 0)
					{
						materialIndices[sub] = materials.Count;
						materials.Add(material);
					}
				}

				cobbleList.Add(new Cobble
				{
					Positions = positions,
					Normals = normals,
					Colours = mesh.colors32,
					Shade = mesh.uv,
					Triangles = triangles,
					Materials = materialIndices,
					Length = maxX - minX,
				});
			}

			return cobbleList.ToArray();
		}

		#endregion
	}
}
