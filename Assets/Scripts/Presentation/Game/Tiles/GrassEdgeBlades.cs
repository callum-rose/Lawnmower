using System;
using System.Collections.Generic;
using Game.Core;
using Game.Terrain;
using Game.UndoSystem;
using Lawnmower.Model;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Tiles
{
	/// <summary>
	/// A sparse fringe of short blades along each side of a grass tile that meets dirt (a mowed-bare tile, or the bare
	/// shore past the solid tiles), straggling out over the dirt so the lawn doesn't stop at a hard line. Each grass
	/// height's container gets its own set of sides, drawn with that height's material, so they show, scale and fade
	/// with the grass. Built in Awake, before the tile registers with <see cref="InstancedTileRenderer"/>, so the sides
	/// are instanced like the grass and switched on and off by setting them active.
	/// </summary>
	internal class GrassEdgeBlades : MonoBehaviour
	{
		[Serializable]
		private class HeightSet
		{
			public GameObject container;
			public Material material;
			[Tooltip("Scales the blades' height, so taller grass straggles out a little taller")]
			public float heightScale = 1;
		}

		// Same blade shape, UV conventions and colour columns as generate_thin_grass.py
		private const float TipWidthRatio = 0.75f;
		private const float NormalUpBias = 2.5f;
		private const int Segments = 3;
		private const int CapPoints = 4;
		private static readonly float[] ColumnCentres = { 0.125f, 0.375f, 0.625f };

		private const int BladesPerSide = 12;
		// From just inside the tile's edge out over the dirt; blades thin out and shorten towards the outer end
		private const float InnerReach = -0.06f;
		private const float OuterReach = 0.12f;
		private const float SpreadPower = 2;
		private static readonly Vector2 InnerHeightRange = new Vector2(0.16f, 0.28f);
		private static readonly Vector2 OuterHeightRange = new Vector2(0.04f, 0.08f);
		private static readonly Vector2 WidthRange = new Vector2(0.06f, 0.1f);
		// Lean away from the tile, splaying out over the dirt
		private static readonly Vector2 LeanRange = new Vector2(0.08f, 0.25f);
		private const float LeanYawSpread = 0.9f;
		// The blades start partway up the colour gradient, or a short blade would be almost all dark base and read as a
		// shard on the dirt; the shortest stop short of the top, so they aren't a field of tiny glowing tips
		private const float BaseV = 0.3f;
		private static readonly Vector2 TipVRange = new Vector2(0.75f, 1);

		// Variants per side, picked at random per tile so a long straight edge doesn't repeat every tile. Picked on
		// Awake, as the instanced batches are keyed on the mesh when the tile registers.
		private const int Variants = 3;

		// Indexed like Sides: north (+z), east (+x), south (-z), west (-x)
		private static readonly GridVector[] Sides = { GridVector.Up, GridVector.Right, GridVector.Down, GridVector.Left };
		private static readonly string[] SideNames = { "North", "East", "South", "West" };
		private static Mesh[,] _meshes;

		[Tooltip("Grass heights 1 to 3: the container that shows each and its grass material")]
		[SerializeField] private HeightSet[] heights;

		private readonly List<Action> _unsubscribes = new List<Action>();
		private GameObject[,] _sides;

		#region Unity

		private void Awake()
		{
			Mesh[,] meshes = GetMeshes();
			_sides = new GameObject[heights.Length, Sides.Length];

			int[] variants = new int[Sides.Length];
			for (int s = 0; s < Sides.Length; s++)
			{
				variants[s] = UnityEngine.Random.Range(0, Variants);
			}

			for (int h = 0; h < heights.Length; h++)
			{
				for (int s = 0; s < Sides.Length; s++)
				{
					GameObject side = new GameObject("Edge " + SideNames[s]);
					side.transform.SetParent(heights[h].container.transform, false);
					side.transform.localScale = new Vector3(1, heights[h].heightScale, 1);
					side.layer = heights[h].container.layer;
					side.SetActive(false);
					side.AddComponent<MeshFilter>().sharedMesh = meshes[s, variants[s]];

					MeshRenderer meshRenderer = side.AddComponent<MeshRenderer>();
					meshRenderer.sharedMaterial = heights[h].material;
					meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
					meshRenderer.receiveShadows = false;
					meshRenderer.lightProbeUsage = LightProbeUsage.Off;
					meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

					_sides[h, s] = side;
				}
			}
		}

		private void OnDestroy()
		{
			Unsubscribe();
		}

		#endregion

		#region API

		/// <summary>Shows a fringe along each side that meets dirt, following the neighbours as they're mowed.</summary>
		public void Connect(Level level, GridVector position)
		{
			Unsubscribe();

			for (int s = 0; s < Sides.Length; s++)
			{
				int side = s;
				Tile neighbour = level.GetTile(position + Sides[s]);
				SetSide(side, IsDirt(neighbour));

				if (neighbour is IReadonlyGrassTile grass)
				{
					UndoableAction<int, int> onHeightChanged = (_, height, _) => SetSide(side, height <= 0);
					grass.GrassHeight.ValueChangedFromTo += onHeightChanged;
					_unsubscribes.Add(() => grass.GrassHeight.ValueChangedFromTo -= onHeightChanged);
				}
			}
		}

		#endregion

		#region Methods

		// Matches where the terrain's top is dirt: mowed-bare grass, and anything off the solid footprint
		private static bool IsDirt(Tile tile)
		{
			return tile switch
			{
				null => true,
				IReadonlyGrassTile grass => grass.GrassHeight.Value <= 0,
				_ => !TerrainManager.IsSolid(tile.Kind),
			};
		}

		private void SetSide(int side, bool show)
		{
			for (int h = 0; h < heights.Length; h++)
			{
				_sides[h, side].SetActive(show);
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

		private static Mesh[,] GetMeshes()
		{
			if (_meshes != null && _meshes[0, 0])
			{
				return _meshes;
			}

			_meshes = new Mesh[Sides.Length, Variants];
			for (int s = 0; s < Sides.Length; s++)
			{
				for (int v = 0; v < Variants; v++)
				{
					_meshes[s, v] = BuildSideMesh(s, new System.Random(s * Variants + v + 1));
				}
			}

			return _meshes;
		}

		// In the tile's space, centred on its pivot: the tile covers [-0.5, 0.5] in x and z
		private static Mesh BuildSideMesh(int side, System.Random rng)
		{
			Vector3 outward = new Vector3(Sides[side].x, 0, Sides[side].y);
			Vector3 along = new Vector3(outward.z, 0, -outward.x);

			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<Vector2> uv0s = new List<Vector2>();
			List<Vector2> uv1s = new List<Vector2>();
			List<Vector2> uv2s = new List<Vector2>();
			List<int> triangles = new List<int>();

			for (int i = 0; i < BladesPerSide; i++)
			{
				// Most blades near the tile, a few straying further out
				float reachT = Mathf.Pow((float)rng.NextDouble(), SpreadPower);
				float reach = Mathf.Lerp(InnerReach, OuterReach, reachT);
				float alongT = (i + (float)rng.NextDouble()) / BladesPerSide - 0.5f;
				Vector3 basePosition = outward * (0.5f + reach) + along * alongT;

				float heightT = (float)rng.NextDouble();
				float height = Mathf.Lerp(Range(InnerHeightRange, heightT), Range(OuterHeightRange, heightT), reachT);
				float width = Range(rng, WidthRange);
				float tipV = Mathf.Lerp(TipVRange.x, TipVRange.y,
					Mathf.InverseLerp(OuterHeightRange.x, InnerHeightRange.y, height));
				float u = ColumnCentres[rng.Next(ColumnCentres.Length)] + Range(rng, new Vector2(-0.04f, 0.04f));

				float yaw = Range(rng, new Vector2(0, Mathf.PI * 2));
				Vector3 sideways = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw));
				Vector3 lean = Quaternion.AngleAxis(Range(rng, new Vector2(-LeanYawSpread, LeanYawSpread)) * Mathf.Rad2Deg,
					Vector3.up) * outward * (height * Range(rng, LeanRange));

				Vector3 faceNormal = Vector3.Cross(Vector3.up, sideways).normalized;
				Vector3 normal = (faceNormal + Vector3.up * NormalUpBias).normalized;
				// From the mesh's origin, unlike the lawn meshes' tile space (their origin is on the tile's corner): the grass
				// shader's intro growth places each blade at its object origin plus uv1
				Vector2 bladeUV = new Vector2(basePosition.x, basePosition.z);

				float capRadius = width * 0.5f * TipWidthRatio;
				float shoulderT = 1 - Mathf.Min(capRadius / height, 0.5f);

				int start = vertices.Count;
				for (int s = 0; s <= Segments; s++)
				{
					float t = shoulderT * s / Segments;
					Vector3 centre = basePosition + Vector3.up * (height * t) + lean * (t * t);
					Vector3 half = sideways * (width * 0.5f * (1 + (TipWidthRatio - 1) * s / Segments));
					vertices.Add(centre - half);
					vertices.Add(centre + half);
					uv0s.Add(new Vector2(u, Mathf.Lerp(BaseV, tipV, t)));
					uv0s.Add(new Vector2(u, Mathf.Lerp(BaseV, tipV, t)));
					uv2s.Add(new Vector2(-1, 0));
					uv2s.Add(new Vector2(1, 0));

					if (s > 0)
					{
						int a = start + (s - 1) * 2;
						triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
					}
				}

				Vector3 shoulder = basePosition + Vector3.up * (height * shoulderT) + lean * (shoulderT * shoulderT);
				Vector3 tangent = (Vector3.up * height + lean * (2 * shoulderT)).normalized;
				int left = start + Segments * 2;
				for (int c = 1; c <= CapPoints; c++)
				{
					float angle = Mathf.PI * c / (CapPoints + 1);
					float capT = Mathf.Sin(angle) * capRadius;
					vertices.Add(shoulder + sideways * (capRadius * Mathf.Cos(angle)) + tangent * capT);
					uv0s.Add(new Vector2(u, Mathf.Lerp(BaseV, tipV, Mathf.Min(1, shoulderT + capT / height))));
					uv2s.Add(new Vector2(Mathf.Cos(angle) < 0 ? -1 : 1, 0));
				}

				// Fan the rounded tip from the shoulder's left corner, as the generator's cap n-gon
				for (int c = left + 1; c < vertices.Count - 1; c++)
				{
					triangles.AddRange(new[] { left, c, c + 1 });
				}

				for (int v = start; v < vertices.Count; v++)
				{
					normals.Add(normal);
					uv1s.Add(bladeUV);
				}
			}

			Mesh mesh = new Mesh
			{
				name = "Grass Edge " + SideNames[side],
				hideFlags = HideFlags.DontUnloadUnusedAsset
			};
			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uv0s);
			mesh.SetUVs(1, uv1s);
			mesh.SetUVs(2, uv2s);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
			return mesh;
		}

		private static float Range(Vector2 range, float t)
		{
			return Mathf.Lerp(range.x, range.y, t);
		}

		private static float Range(System.Random rng, Vector2 range)
		{
			return Range(range, (float)rng.NextDouble());
		}

		#endregion
	}
}
