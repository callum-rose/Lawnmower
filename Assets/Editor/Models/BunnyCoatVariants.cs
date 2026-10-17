using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Mowers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lawnmower.Editor.Models
{
	/// <summary>
	/// Builds recoloured EggBunny mowers from real rabbits' coats. The EggBunny mesh UVs collapse each part onto one
	/// palette swatch, so each variant gets copies of the meshes with a real unwrap, and a texture baked by sampling the
	/// coat function at every texel's point on the bunny. Re-running the menu item regenerates the textures, meshes,
	/// materials, prefab variants and mower data in place (keeping their GUIDs).
	/// </summary>
	public static class BunnyCoatVariants
	{
		private const string SourcePrefabPath = "Assets/Prefabs/Game/Mowers/Complete/EggBunny.prefab";
		private const string SourceMaterialPath = "Assets/Materials/Game/Mowers/EggBunny.mat";
		private const string PrefabFolder = "Assets/Prefabs/Game/Mowers/Complete/";
		private const string MaterialFolder = "Assets/Materials/Game/Mowers/";
		private const string TextureFolder = "Assets/Textures/Game/Mowers/";
		private const string MeshFolder = "Assets/Models/Mower/Generated/";
		private const string MowerDataFolder = "Assets/ScriptableObjects/Game/Mower/Data/";
		private const string MowerDataManagerPath = "Assets/ScriptableObjects/Game/Mower/Functionals/MowerDataManager.asset";
		private const string MainTexProperty = "_MainTex2";

		private const int TextureSize = 1024;
		private const int Supersample = 3;
		private const int BleedPixels = 16;
		private static readonly Vector2 MuzzleFlag = new Vector2(100f, 0f);

		// Where each part's unwrap is packed in the coat atlas (uv space).
		private static readonly Dictionary<string, Rect> AtlasRects = new Dictionary<string, Rect>
		{
			{ "Body", new Rect(0f, 0f, 0.75f, 0.75f) },
			{ "ear_left", new Rect(0.75f, 0.5f, 0.25f, 0.25f) },
			{ "ear_right", new Rect(0.75f, 0.25f, 0.25f, 0.25f) },
			{ "tail", new Rect(0.75f, 0f, 0.25f, 0.25f) },
			{ "ear_left_patch", new Rect(0f, 0.75f, 0.125f, 0.125f) },
			{ "ear_right_patch", new Rect(0.125f, 0.75f, 0.125f, 0.125f) },
			{ "foot_front_left", new Rect(0.25f, 0.75f, 0.125f, 0.125f) },
			{ "foot_front_right", new Rect(0.375f, 0.75f, 0.125f, 0.125f) },
			{ "foot_back_left", new Rect(0.5f, 0.75f, 0.125f, 0.125f) },
			{ "foot_back_right", new Rect(0.625f, 0.75f, 0.125f, 0.125f) }
		};

		// The coat functions below are authored in egg_bunny.blend's space: +x front, +z up, body spanning these bounds.
		private const float BlendBodyCentreX = -0.045f;
		private const float BlendBodyCentreZ = -0.01f;
		private const float BlendBodyHeight = 1.92f;

		private enum Part
		{
			Body,
			Muzzle,
			Ear,
			EarPatch,
			FootFront,
			FootBack,
			Tail
		}

		private readonly struct BakeTriangle
		{
			public readonly Vector2 A, B, C;
			public readonly Vector3 PA, PB, PC;
			public readonly Part Part;

			public BakeTriangle(Vector2 a, Vector2 b, Vector2 c, Vector3 pa, Vector3 pb, Vector3 pc, Part part)
			{
				A = a;
				B = b;
				C = c;
				PA = pa;
				PB = pb;
				PC = pc;
				Part = part;
			}
		}

		private sealed class Coat
		{
			public string Id;
			public string DisplayName;
			public string MowerGuid;
			public string[] Palette;
			public Func<Part, Vector3, int> Swatch;
		}

		private static readonly Coat[] coats =
		{
			new Coat
			{
				// Dutch-marked tortoiseshell agouti: dark head, white collar and feet, mottled orange/grey rump.
				Id = "PatchworkBunny",
				DisplayName = "Patchwork Bunny",
				MowerGuid = "74c3a9e4249b4da9a47b171f5bd88dc0",
				Palette = new[] { "#6F6156", "#8E7E70", "#F3F0E9", "#C79A68", "#857668", "#B8A092", "#4E433B", "#62554B" },
				Swatch = PatchworkSwatch
			},
			new Coat
			{
				// Sandy agouti: warm fawn all over, darker back and ear rims, cream belly and feet, white scut.
				Id = "SandyBunny",
				DisplayName = "Sandy Bunny",
				MowerGuid = "5b29802830164088898299586da44fd1",
				Palette = new[] { "#C8AA81", "#B5966F", "#E6D7BB", "#DCC6A0", "#94775A", "#DCB8A6", "#EEEAE3", "#E3D3B8" },
				Swatch = SandySwatch
			}
		};

		[MenuItem("Bunny Heaven/Build Bunny Coat Variants")]
		public static void BuildAll()
		{
			GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
			Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
			if (source == null || sourceMaterial == null)
			{
				throw new FileNotFoundException("EggBunny prefab or material not found");
			}

			if (!AssetDatabase.IsValidFolder(MeshFolder.TrimEnd('/')))
			{
				AssetDatabase.CreateFolder("Assets/Models/Mower", "Generated");
			}

			foreach (Coat coat in coats)
			{
				Build(coat, source, sourceMaterial);
			}

			AssetDatabase.SaveAssets();
		}

		#region Coats

		private static int PatchworkSwatch(Part part, Vector3 p)
		{
			float ay = Mathf.Abs(p.y);
			switch (part)
			{
				case Part.Ear: return p.z < 1.6f ? 0 : 6;
				case Part.EarPatch: return 5;
				case Part.FootFront:
				case Part.FootBack: return 2;
				case Part.Tail: return p.z > -0.4f ? 4 : 2;
				case Part.Muzzle: return 1;
			}

			float head = -0.12f - 0.5f * p.x + 0.08f * (Fbm(p.x, ay, p.z, 31, 2.5f) - 0.5f);
			if (p.z > head)
			{
				return Fbm(p.x, ay, p.z, 3, 2.2f) > 0.62f ? 7 : 0;
			}

			if (p.z > head - 0.45f)
			{
				return 2;
			}

			float n = Fbm(p.x + 0.2f, ay, p.z, 11, 1.3f);
			return n > 0.58f ? 3 : n < 0.36f ? 6 : 4;
		}

		private static int SandySwatch(Part part, Vector3 p)
		{
			float ay = Mathf.Abs(p.y);
			switch (part)
			{
				case Part.Ear: return p.z > 1.68f ? 4 : 0;
				case Part.EarPatch: return 5;
				case Part.FootFront:
				case Part.FootBack: return 7;
				case Part.Tail: return 6;
				case Part.Muzzle: return 3;
			}

			if (p.z < -0.62f + 0.15f * p.x)
			{
				return 2;
			}

			return p.z > 0.35f - 0.45f * p.x + 0.15f * (Fbm(p.x, ay, p.z, 23, 2f) - 0.5f) ? 1 : 0;
		}

		#endregion

		#region Build

		private static void Build(Coat coat, GameObject source, Material sourceMaterial)
		{
			List<BakeTriangle> triangles = new List<BakeTriangle>();
			Dictionary<string, Mesh> meshes = WriteMeshes(coat, source, triangles);
			Texture2D texture = WriteTexture(coat, triangles);
			Material material = WriteMaterial(coat, sourceMaterial, texture);
			GameObject prefab = WritePrefab(coat, source, sourceMaterial, material, meshes);
			WriteMowerData(coat, prefab);
		}

		/// <summary>
		/// Copies each part's mesh with a fresh unwrap in uv0, packed into that part's rect of the coat atlas. The face
		/// decal's FaceMap stays in uv1, and the original palette UVs only survive long enough to find the face plate.
		/// </summary>
		private static Dictionary<string, Mesh> WriteMeshes(Coat coat, GameObject source, List<BakeTriangle> bake)
		{
			string folder = MeshFolder + coat.Id;
			if (!AssetDatabase.IsValidFolder(folder))
			{
				AssetDatabase.CreateFolder(MeshFolder.TrimEnd('/'), coat.Id);
			}

			Dictionary<string, Mesh> result = new Dictionary<string, Mesh>();
			BunnyFrame frame = BunnyFrame.From(source);
			UnwrapParam.SetDefaults(out UnwrapParam unwrap);
			unwrap.packMargin = 0.02f;

			foreach ((Transform part, Mesh sourceMesh) in MeshParts(source))
			{
				if (!AtlasRects.TryGetValue(sourceMesh.name, out Rect rect))
				{
					throw new InvalidOperationException("No coat atlas rect for EggBunny mesh " + sourceMesh.name);
				}

				// The unwrap keeps uv0 but drops other channels, so FaceMap rides in uv0 (shifted for face-plate verts).
				Mesh unwrapped = Object.Instantiate(sourceMesh);
				List<Vector2> carried = new List<Vector2>();
				List<Vector2> palette = new List<Vector2>();
				unwrapped.GetUVs(1, carried);
				unwrapped.GetUVs(0, palette);
				for (int i = 0; i < carried.Count; i++)
				{
					carried[i] += IsMuzzleSwatch(palette[i].x) ? MuzzleFlag : Vector2.zero;
				}

				unwrapped.SetUVs(0, carried);
				Unwrapping.GenerateSecondaryUVSet(unwrapped, unwrap);

				List<Vector2> atlas = new List<Vector2>();
				unwrapped.GetUVs(0, carried);
				unwrapped.GetUVs(1, atlas);
				bool[] muzzleVertex = new bool[carried.Count];
				for (int i = 0; i < carried.Count; i++)
				{
					muzzleVertex[i] = carried[i].x > MuzzleFlag.x * 0.5f;
					carried[i] -= muzzleVertex[i] ? MuzzleFlag : Vector2.zero;
					atlas[i] = rect.min + Vector2.Scale(atlas[i], rect.size);
				}

				unwrapped.SetUVs(0, atlas);
				unwrapped.SetUVs(1, carried);

				Matrix4x4 toRoot = source.transform.worldToLocalMatrix * part.localToWorldMatrix;
				Vector3[] blend = unwrapped.vertices.Select(v => frame.ToBlend(toRoot.MultiplyPoint3x4(v))).ToArray();
				Part kind = PartOf(part.name);
				int[] indices = unwrapped.triangles;
				for (int t = 0; t < indices.Length; t += 3)
				{
					int a = indices[t], b = indices[t + 1], c = indices[t + 2];
					bool muzzle = kind == Part.Body && muzzleVertex[a] && muzzleVertex[b] && muzzleVertex[c];
					bake.Add(new BakeTriangle(atlas[a], atlas[b], atlas[c], blend[a], blend[b], blend[c], muzzle ? Part.Muzzle : kind));
				}

				string path = folder + "/" + sourceMesh.name + ".asset";
				Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
				if (mesh == null)
				{
					unwrapped.name = sourceMesh.name;
					AssetDatabase.CreateAsset(unwrapped, path);
					mesh = unwrapped;
				}
				else
				{
					CopyMesh(unwrapped, mesh);
					Object.DestroyImmediate(unwrapped);
					EditorUtility.SetDirty(mesh);
				}

				result[sourceMesh.name] = mesh;
			}

			AssetDatabase.SaveAssets();
			return result;
		}

		/// <summary>
		/// Rasterises every part into the coat atlas, sampling the coat at each texel's point on the bunny. Several
		/// samples per texel anti-alias the pattern's edges, then the islands are bled outwards so filtering never
		/// reaches the empty background.
		/// </summary>
		private static Texture2D WriteTexture(Coat coat, List<BakeTriangle> triangles)
		{
			Color[] palette = coat.Palette.Select(hex => ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta).ToArray();
			Color[] sum = new Color[TextureSize * TextureSize];
			int[] count = new int[sum.Length];

			foreach (BakeTriangle tri in triangles)
			{
				Vector2 a = tri.A * TextureSize, b = tri.B * TextureSize, c = tri.C * TextureSize;
				float area = Cross(b - a, c - a);
				if (Mathf.Abs(area) < 1e-9f)
				{
					continue;
				}

				int xMin = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x)), 0, TextureSize - 1);
				int xMax = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x, c.x)), 0, TextureSize - 1);
				int yMin = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y)), 0, TextureSize - 1);
				int yMax = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y, c.y)), 0, TextureSize - 1);

				for (int y = yMin; y <= yMax; y++)
				for (int x = xMin; x <= xMax; x++)
				for (int s = 0; s < Supersample * Supersample; s++)
				{
					Vector2 p = new Vector2(x + (s % Supersample + 0.5f) / Supersample, y + (s / Supersample + 0.5f) / Supersample);
					float wa = Cross(b - p, c - p) / area;
					float wb = Cross(c - p, a - p) / area;
					float wc = 1f - wa - wb;
					if (wa < 0f || wb < 0f || wc < 0f)
					{
						continue;
					}

					int i = y * TextureSize + x;
					sum[i] += palette[coat.Swatch(tri.Part, tri.PA * wa + tri.PB * wb + tri.PC * wc)];
					count[i]++;
				}
			}

			Color[] pixels = new Color[sum.Length];
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = count[i] > 0 ? sum[i] / count[i] : Color.clear;
			}

			Bleed(pixels, count.Select(n => n > 0).ToArray());
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i].a = 1f;
			}

			string path = TextureFolder + ToSnake(coat.Id) + "_tex.png";
			Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
			texture.SetPixels(pixels);
			File.WriteAllBytes(path, texture.EncodeToPNG());
			Object.DestroyImmediate(texture);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.mipmapEnabled = true;
			importer.alphaSource = TextureImporterAlphaSource.None;
			importer.maxTextureSize = TextureSize;
			importer.filterMode = FilterMode.Bilinear;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.SaveAndReimport();
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		private static void Bleed(Color[] pixels, bool[] filled)
		{
			for (int pass = 0; pass < BleedPixels; pass++)
			{
				bool[] next = (bool[])filled.Clone();
				for (int y = 0; y < TextureSize; y++)
				for (int x = 0; x < TextureSize; x++)
				{
					int i = y * TextureSize + x;
					if (filled[i])
					{
						continue;
					}

					Color total = Color.clear;
					int n = 0;
					for (int dy = -1; dy <= 1; dy++)
					for (int dx = -1; dx <= 1; dx++)
					{
						int nx = x + dx, ny = y + dy;
						if (nx >= 0 && ny >= 0 && nx < TextureSize && ny < TextureSize && filled[ny * TextureSize + nx])
						{
							total += pixels[ny * TextureSize + nx];
							n++;
						}
					}

					if (n > 0)
					{
						pixels[i] = total / n;
						next[i] = true;
					}
				}

				Array.Copy(next, filled, filled.Length);
			}
		}

		private static Material WriteMaterial(Coat coat, Material sourceMaterial, Texture2D texture)
		{
			string path = MaterialFolder + coat.Id + ".mat";
			Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (material == null)
			{
				AssetDatabase.CopyAsset(SourceMaterialPath, path);
				material = AssetDatabase.LoadAssetAtPath<Material>(path);
			}

			material.SetTexture(MainTexProperty, texture);
			EditorUtility.SetDirty(material);
			return material;
		}

		private static GameObject WritePrefab(Coat coat, GameObject source, Material sourceMaterial, Material material, Dictionary<string, Mesh> meshes)
		{
			string path = PrefabFolder + coat.Id + ".prefab";
			bool exists = File.Exists(path);
			GameObject root = exists
				? PrefabUtility.LoadPrefabContents(path)
				: (GameObject)PrefabUtility.InstantiatePrefab(source);

			foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
			{
				filter.sharedMesh = Swap(filter.sharedMesh, meshes);
			}

			foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				skinned.sharedMesh = Swap(skinned.sharedMesh, meshes);
			}

			// Renderers plus the face components, which animate their material asset directly.
			foreach (Component component in root.GetComponentsInChildren<Component>(true))
			{
				if (component == null)
				{
					continue;
				}

				SerializedObject serialized = new SerializedObject(component);
				SerializedProperty property = serialized.GetIterator();
				while (property.Next(true))
				{
					if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == sourceMaterial)
					{
						property.objectReferenceValue = material;
					}
				}

				serialized.ApplyModifiedPropertiesWithoutUndo();
			}

			GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
			if (exists)
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
			else
			{
				Object.DestroyImmediate(root);
			}

			return saved;
		}

		private static void WriteMowerData(Coat coat, GameObject prefab)
		{
			string path = MowerDataFolder + coat.Id + ".asset";
			MowerData data = AssetDatabase.LoadAssetAtPath<MowerData>(path);
			if (data == null)
			{
				data = ScriptableObject.CreateInstance<MowerData>();
				AssetDatabase.CreateAsset(data, path);
			}

			SerializedObject serialized = new SerializedObject(data);
			serialized.FindProperty("guid").stringValue = coat.MowerGuid;
			serialized.FindProperty("name").stringValue = coat.DisplayName;
			serialized.FindProperty("prefab").objectReferenceValue = prefab.GetComponent<MowerObject>();
			serialized.ApplyModifiedPropertiesWithoutUndo();

			SerializedObject manager = new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(MowerDataManagerPath));
			SerializedProperty list = manager.FindProperty("mowerData");
			for (int i = 0; i < list.arraySize; i++)
			{
				if (list.GetArrayElementAtIndex(i).objectReferenceValue == data)
				{
					return;
				}
			}

			list.InsertArrayElementAtIndex(list.arraySize);
			list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
			manager.ApplyModifiedPropertiesWithoutUndo();
		}

		#endregion

		#region Helpers

		private static IEnumerable<(Transform part, Mesh mesh)> MeshParts(GameObject root)
		{
			foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
			{
				if (filter.sharedMesh != null) yield return (filter.transform, filter.sharedMesh);
			}

			foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
			{
				if (skinned.sharedMesh != null) yield return (skinned.transform, skinned.sharedMesh);
			}
		}

		private static Mesh Swap(Mesh mesh, Dictionary<string, Mesh> meshes) =>
			mesh != null && meshes.TryGetValue(mesh.name, out Mesh swapped) ? swapped : mesh;

		private static Part PartOf(string name)
		{
			if (name.EndsWith("_patch")) return Part.EarPatch;
			if (name.StartsWith("ear_")) return Part.Ear;
			if (name.StartsWith("foot_front")) return Part.FootFront;
			if (name.StartsWith("foot_back")) return Part.FootBack;
			if (name.StartsWith("tail")) return Part.Tail;
			return Part.Body;
		}

		/// <summary>The EggBunny palette's second column is the face plate the decal face is drawn on.</summary>
		private static bool IsMuzzleSwatch(float u) => u >= 0.25f && u < 0.5f;

		/// <summary>Through the Mesh API rather than CopySerialized, which leaves the GPU copy stale.</summary>
		private static void CopyMesh(Mesh source, Mesh target)
		{
			List<Vector3> v3 = new List<Vector3>();
			List<Vector4> v4 = new List<Vector4>();
			List<Vector2> v2 = new List<Vector2>();

			target.Clear();
			target.indexFormat = source.indexFormat;
			source.GetVertices(v3);
			target.SetVertices(v3);
			source.GetNormals(v3);
			target.SetNormals(v3);
			source.GetTangents(v4);
			target.SetTangents(v4);
			for (int channel = 0; channel < 2; channel++)
			{
				source.GetUVs(channel, v2);
				target.SetUVs(channel, v2);
			}

			target.subMeshCount = source.subMeshCount;
			for (int sub = 0; sub < source.subMeshCount; sub++)
			{
				target.SetTriangles(source.GetTriangles(sub), sub);
			}

			target.RecalculateBounds();
		}

		private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

		private static string ToSnake(string id) =>
			string.Concat(id.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

		/// <summary>Maps the prefab's root space onto egg_bunny.blend's, found from the muzzle (front) and ears (up).</summary>
		private readonly struct BunnyFrame
		{
			private readonly Vector3 centre;
			private readonly Vector3 front;
			private readonly Vector3 up;
			private readonly Vector3 side;
			private readonly float scale;

			private BunnyFrame(Vector3 centre, Vector3 front, Vector3 up, float scale)
			{
				this.centre = centre;
				this.front = front;
				this.up = up;
				side = Vector3.Cross(up, front);
				this.scale = scale;
			}

			public static BunnyFrame From(GameObject root)
			{
				Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
				List<Vector3> body = new List<Vector3>();
				List<Vector3> muzzle = new List<Vector3>();
				List<Vector3> ears = new List<Vector3>();

				foreach ((Transform part, Mesh mesh) in MeshParts(root))
				{
					Matrix4x4 m = toRoot * part.localToWorldMatrix;
					Vector3[] vertices = mesh.vertices;
					Part kind = PartOf(part.name);
					if (kind == Part.Ear)
					{
						ears.AddRange(vertices.Select(m.MultiplyPoint3x4));
					}
					else if (kind == Part.Body)
					{
						body.AddRange(vertices.Select(m.MultiplyPoint3x4));
						Vector2[] uv = mesh.uv;
						muzzle.AddRange(vertices.Where((_, i) => uv[i].x >= 0.25f && uv[i].x < 0.5f).Select(m.MultiplyPoint3x4));
					}
				}

				Bounds bounds = new Bounds(body[0], Vector3.zero);
				foreach (Vector3 point in body)
				{
					bounds.Encapsulate(point);
				}

				Vector3 up = Dominant(Average(ears) - bounds.center);
				Vector3 front = Dominant(Average(muzzle) - bounds.center);
				float height = Mathf.Abs(Vector3.Dot(bounds.size, up));
				return new BunnyFrame(bounds.center, front, up, BlendBodyHeight / height);
			}

			public Vector3 ToBlend(Vector3 rootPoint)
			{
				Vector3 d = rootPoint - centre;
				return new Vector3(
					Vector3.Dot(d, front) * scale + BlendBodyCentreX,
					Vector3.Dot(d, side) * scale,
					Vector3.Dot(d, up) * scale + BlendBodyCentreZ);
			}

			private static Vector3 Average(List<Vector3> points) =>
				points.Aggregate(Vector3.zero, (sum, p) => sum + p) / points.Count;

			private static Vector3 Dominant(Vector3 v)
			{
				Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
				if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0, 0);
				if (a.y >= a.z) return new Vector3(0, Mathf.Sign(v.y), 0);
				return new Vector3(0, 0, Mathf.Sign(v.z));
			}
		}

		#endregion

		#region Noise

		private static float Hash(int x, int y, int z, int seed)
		{
			unchecked
			{
				uint n = (uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 2147483647u + (uint)seed * 144665u;
				n = (n ^ (n >> 13)) * 1274126177u;
				return ((n ^ (n >> 16)) & 0xffff) / 65535f;
			}
		}

		private static float Smooth(float t) => t * t * (3 - 2 * t);

		private static float ValueNoise(float x, float y, float z, int seed)
		{
			int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y), iz = Mathf.FloorToInt(z);
			float fx = Smooth(x - ix), fy = Smooth(y - iy), fz = Smooth(z - iz);

			float Corner(int i, int j, int k) => Hash(ix + i, iy + j, iz + k, seed);
			float Edge(int i, int j) => Mathf.Lerp(Corner(i, j, 0), Corner(i, j, 1), fz);
			float Face(int i) => Mathf.Lerp(Edge(i, 0), Edge(i, 1), fy);
			return Mathf.Lerp(Face(0), Face(1), fx);
		}

		private static float Fbm(float x, float y, float z, int seed, float frequency) =>
			0.6f * ValueNoise(x * frequency, y * frequency, z * frequency, seed)
			+ 0.4f * ValueNoise(x * frequency * 2.1f, y * frequency * 2.1f, z * frequency * 2.1f, seed + 7);

		#endregion
	}
}
