using System.Collections.Generic;
using UnityEngine;

namespace Game.Tiles
{
	// Emits clippings cut from the thin grass blades: the top of a blade from the new grass height up to its rounded tip
	[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
	internal class GrassParticlesShaper : MonoBehaviour
	{
		// Mirror TILES and the blade constants in Assets/Models/Tile/Grass/Blender/generate_thin_grass.py, indexed by grass height
		private static readonly float[] TileHeights = { 0, 0.4f, 0.5f, 0.6f };
		private static readonly Vector2[] BladeWidthRanges =
		{
			Vector2.zero, new Vector2(0.059f, 0.088f), new Vector2(0.092f, 0.138f), new Vector2(0.12f, 0.18f)
		};

		private static readonly Vector2 BladeHeightRange = new Vector2(0.55f, 1);
		private static readonly float[] ColumnCentres = { 0.125f, 0.375f, 0.625f };
		private const float TipWidthRatio = 0.75f;
		private const float NormalUpBias = 2.5f;
		private const int Segments = 2;
		private const int CapPoints = 5;
		private const int VariantsPerHeight = 8;

		private static readonly Dictionary<int, Mesh[]> ClippingMeshes = new Dictionary<int, Mesh[]>();

		private ParticleSystem _particles;
		private ParticleSystemRenderer _renderer;

		private void Awake()
		{
			_particles = GetComponent<ParticleSystem>();
			_renderer = GetComponent<ParticleSystemRenderer>();
		}

		/// <param name="grassHeight">The height the grass was cut from</param>
		public void Set(int grassHeight)
		{
			grassHeight = Mathf.Clamp(grassHeight, 1, GrassTile.MaxGrassHeight);

			_renderer.SetMeshes(GetClippingMeshes(grassHeight));

			// Emit from the middle of the removed band of an average blade
			float averageBladeHeight = (BladeHeightRange.x + BladeHeightRange.y) * 0.5f;
			float cutBandCentre = (TileHeights[grassHeight] + TileHeights[grassHeight - 1]) * 0.5f * averageBladeHeight;
			ParticleSystem.ShapeModule shapeModule = _particles.shape;
			shapeModule.position = Vector3.up * cutBandCentre;
		}

		private static Mesh[] GetClippingMeshes(int grassHeight)
		{
			if (ClippingMeshes.TryGetValue(grassHeight, out Mesh[] meshes) && meshes[0])
			{
				return meshes;
			}

			System.Random rng = new System.Random(grassHeight);
			meshes = new Mesh[VariantsPerHeight];
			for (int i = 0; i < VariantsPerHeight; i++)
			{
				meshes[i] = BuildClippingMesh(grassHeight, rng);
			}

			ClippingMeshes[grassHeight] = meshes;
			return meshes;
		}

		// Same blade shape and UV conventions as generate_thin_grass.py, keeping only the part above the cut.
		// Particles only receive uv0 (default vertex streams), so uv0.y runs from the cut to 1 to keep the tip colour and glow.
		private static Mesh BuildClippingMesh(int grassHeight, System.Random rng)
		{
			float bladeHeight = TileHeights[grassHeight] * Range(rng, BladeHeightRange);
			float cutT = TileHeights[grassHeight - 1] / TileHeights[grassHeight];
			float width = Range(rng, BladeWidthRanges[grassHeight]);
			float u = ColumnCentres[rng.Next(ColumnCentres.Length)] + Range(rng, new Vector2(-0.04f, 0.04f));

			float length = bladeHeight * (1 - cutT);
			float tipHalfWidth = width * 0.5f * TipWidthRatio;
			float capHeight = Mathf.Min(tipHalfWidth, length * 0.5f);
			float shoulderT = 1 - capHeight / bladeHeight;
			// Centre the clipping on its pivot so it tumbles about its middle
			float centreY = bladeHeight * (cutT + 1) * 0.5f;

			List<Vector3> vertices = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();
			List<int> triangles = new List<int>();

			for (int s = 0; s <= Segments; s++)
			{
				float t = Mathf.Lerp(cutT, shoulderT, (float)s / Segments);
				float halfWidth = width * 0.5f * (1 + (TipWidthRatio - 1) * t);
				float y = bladeHeight * t - centreY;
				vertices.Add(new Vector3(-halfWidth, y, 0));
				vertices.Add(new Vector3(halfWidth, y, 0));
				uvs.Add(new Vector2(u, t));
				uvs.Add(new Vector2(u, t));

				if (s > 0)
				{
					int a = (s - 1) * 2;
					triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
				}
			}

			int left = Segments * 2;
			float shoulderHalfWidth = vertices[left + 1].x;
			float shoulderY = vertices[left].y;
			for (int i = 1; i <= CapPoints; i++)
			{
				float angle = Mathf.PI * i / (CapPoints + 1);
				float capT = Mathf.Sin(angle) * capHeight;
				vertices.Add(new Vector3(Mathf.Cos(angle) * shoulderHalfWidth, shoulderY + capT, 0));
				uvs.Add(new Vector2(u, shoulderT + capT / bladeHeight));
			}

			// Fan the rounded tip from the shoulder's left corner, as the generator's cap n-gon
			for (int i = left + 1; i < vertices.Count - 1; i++)
			{
				triangles.AddRange(new[] { left, i, i + 1 });
			}

			Vector3 normal = new Vector3(0, NormalUpBias, 1).normalized;
			Vector3[] normals = new Vector3[vertices.Count];
			for (int i = 0; i < normals.Length; i++)
			{
				normals[i] = normal;
			}

			Mesh mesh = new Mesh
			{
				name = $"Grass Clipping {grassHeight}",
				hideFlags = HideFlags.DontUnloadUnusedAsset
			};
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
			mesh.normals = normals;
			mesh.RecalculateBounds();
			return mesh;
		}

		private static float Range(System.Random rng, Vector2 range)
		{
			return Mathf.Lerp(range.x, range.y, (float)rng.NextDouble());
		}
	}
}
