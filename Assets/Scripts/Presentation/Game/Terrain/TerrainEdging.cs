using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Terrain
{
	/// <summary>
	/// Rings the terrain's shoreline with voxel cobbles, combined into one mesh. Cobbles are placed at even spacing along
	/// each straight side and corner arc of the <see cref="TerrainFootprint"/>, inset from the outline and sunk into the
	/// rounded top edge, each turned to follow the shore in 5° steps (VoxelStyle.md R6). Which cobble goes where is
	/// hashed from its position, so a level always looks the same.
	/// </summary>
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	internal class TerrainEdging : MonoBehaviour
	{
		private const float RotationStep = 5;

		[Tooltip("The cobbles model: one mesh per cobble, with Palette_<hex> materials and baked AO vertex colours")]
		[SerializeField] private GameObject cobbles;

		[Tooltip("Distance between cobble centres along the shoreline, in tiles")]
		[SerializeField, Min(0.05f)] private float spacing = 0.32f;

		[Tooltip("How far each cobble's centre sits inside the outline, in tiles")]
		[SerializeField] private float inset = 0.14f;

		[Tooltip("How far each cobble's base sits below the terrain top")]
		[SerializeField] private float sink = 0.1f;

		[Tooltip("Random turn either side of the shoreline, in whole 5° steps")]
		[SerializeField, Range(0, 4)] private int turnSteps = 2;

		[SerializeField] private uint seed = 1;

		private struct Cobble
		{
			public Vector3[] Positions;
			public Vector3[] Normals;
			public Color32[] Colours;
			public int[] Triangles;
		}

		private Cobble[] _cobbles;
		private Mesh _mesh;

		private readonly List<Vector3> _positions = new List<Vector3>();
		private readonly List<Vector3> _normals = new List<Vector3>();
		private readonly List<Color32> _colours = new List<Color32>();
		private readonly List<int> _triangles = new List<int>();

		#region Unity

		private void Awake()
		{
			_mesh = new Mesh { name = "TerrainEdging", indexFormat = IndexFormat.UInt32 };
			GetComponent<MeshFilter>().sharedMesh = _mesh;
			_cobbles = LoadCobbles(cobbles);
		}

		private void OnDestroy()
		{
			Destroy(_mesh);
		}

		#endregion

		#region API

		public void Build(TerrainFootprint footprint, float topHeight)
		{
			_positions.Clear();
			_normals.Clear();
			_colours.Clear();
			_triangles.Clear();

			float y = topHeight - sink;

			foreach (float4 segment in footprint.Segments)
			{
				float2 a = segment.xy;
				float2 b = segment.zw;
				float2 tangent = math.normalize(b - a);
				int count = Count(math.distance(a, b));

				for (int i = 0; i < count; i++)
				{
					float2 point = math.lerp(a, b, (i + 0.5f) / count);
					AddCobble(footprint, point, tangent, y);
				}
			}

			foreach (FootprintArc arc in footprint.Arcs)
			{
				int count = Count(arc.Radius * math.PI * 0.5f);

				for (int i = 0; i < count; i++)
				{
					float angle = (i + 0.5f) / count * math.PI * 0.5f;
					float2 direction = new float2(-arc.Facing.x * math.cos(angle), -arc.Facing.y * math.sin(angle));
					float2 point = arc.Centre + direction * arc.Radius;
					AddCobble(footprint, point, new float2(-direction.y, direction.x), y);
				}
			}

			_mesh.Clear();
			_mesh.SetVertices(_positions);
			_mesh.SetNormals(_normals);
			_mesh.SetColors(_colours);
			_mesh.SetTriangles(_triangles, 0);
			_mesh.RecalculateBounds();
		}

		#endregion

		#region Methods

		private int Count(float length)
		{
			return Mathf.Max(1, Mathf.RoundToInt(length / spacing));
		}

		private void AddCobble(TerrainFootprint footprint, float2 point, float2 tangent, float y)
		{
			// The outline's own winding isn't known here, so find which side of it is solid
			float2 normal = new float2(tangent.y, -tangent.x);
			if (footprint.IsInside(point + normal * 0.02f))
			{
				normal = -normal;
			}

			float2 centre = point - normal * inset;

			uint hash = math.hash(new int3((int2)math.round(centre * 1000), (int)seed));
			Unity.Mathematics.Random random = new Unity.Mathematics.Random(hash | 1);
			Cobble cobble = _cobbles[random.NextInt(_cobbles.Length)];

			float yaw = math.degrees(math.atan2(-tangent.y, tangent.x));
			yaw = math.round(yaw / RotationStep) * RotationStep + random.NextInt(-turnSteps, turnSteps + 1) * RotationStep;

			Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(centre.x, y, centre.y), Quaternion.Euler(0, yaw, 0), Vector3.one);
			int first = _positions.Count;

			for (int i = 0; i < cobble.Positions.Length; i++)
			{
				_positions.Add(matrix.MultiplyPoint3x4(cobble.Positions[i]));
				_normals.Add(matrix.MultiplyVector(cobble.Normals[i]));
				_colours.Add(cobble.Colours[i]);
			}

			foreach (int index in cobble.Triangles)
			{
				_triangles.Add(first + index);
			}
		}

		/// <summary>
		/// Flattens each cobble in the model into the root's space, packing its palette colour (from its material's
		/// <c>Palette_&lt;hex&gt;</c> name) into the vertex RGB and its baked AO into A.
		/// </summary>
		private static Cobble[] LoadCobbles(GameObject model)
		{
			List<Cobble> result = new List<Cobble>();
			Matrix4x4 toRoot = model.transform.worldToLocalMatrix;

			foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
			{
				Mesh mesh = filter.sharedMesh;
				Material[] materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
				Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

				Vector3[] positions = mesh.vertices;
				Vector3[] normals = mesh.normals;
				Color32[] ao = mesh.colors32;
				Color32[] colours = new Color32[positions.Length];
				List<int> triangles = new List<int>();

				for (int i = 0; i < positions.Length; i++)
				{
					positions[i] = matrix.MultiplyPoint3x4(positions[i]);
					normals[i] = matrix.MultiplyVector(normals[i]).normalized;
				}

				for (int sub = 0; sub < mesh.subMeshCount; sub++)
				{
					Color32 palette = PaletteColour(materials[Mathf.Min(sub, materials.Length - 1)]);
					int[] subTriangles = mesh.GetTriangles(sub);
					triangles.AddRange(subTriangles);

					foreach (int index in subTriangles)
					{
						byte occlusion = ao.Length > 0 ? ao[index].r : byte.MaxValue;
						colours[index] = new Color32(palette.r, palette.g, palette.b, occlusion);
					}
				}

				result.Add(new Cobble
				{
					Positions = positions,
					Normals = normals,
					Colours = colours,
					Triangles = triangles.ToArray(),
				});
			}

			return result.ToArray();
		}

		private static Color32 PaletteColour(Material material)
		{
			const string prefix = "Palette_";
			string name = material.name;
			int start = name.IndexOf(prefix, System.StringComparison.Ordinal);

			if (start >= 0 && ColorUtility.TryParseHtmlString("#" + name.Substring(start + prefix.Length, 6), out Color colour))
			{
				return colour;
			}

			Debug.LogError($"Cobble material {name} isn't named Palette_<hex>", material);
			return Color.magenta;
		}

		#endregion
	}
}
