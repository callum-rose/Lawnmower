using System;
using System.Collections.Generic;
using System.IO;
using Game.Terrain;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// Renders the Water shader in a test pond (the level's SDF terrain and grass tiles around primitive rocks, fish, plants,
// a sand floor and lily pads) from the
// game camera, for judging the look outside play mode. Run in batch mode:
//   Unity -batchmode -quit -projectPath . -executeMethod WaterPreviewRenderer.Render -logFile <log>
// Environment variables: WATER_OUT (output folder), WATER_VIDEO=1 (raw RGB24 clip of the close shot instead of stills,
// encode with ffmpeg -f rawvideo -pix_fmt rgb24 -s WxH -r 30 -i Water.rgb -vf vflip out.mp4), WATER_W (width),
// WATER_SECONDS, WATER_FPS, WATER_SHOT (close|wide), WATER_WARMUP (ripple seconds simulated first),
// WATER_REFLECT=0 (no planar reflection), WATER_SOURCES=0 (no constant ripple emitters, only splashes, as in the game),
// WATER_DEBUG=1 (dump the ripple height field over time instead).
public static class WaterPreviewRenderer
{
	static Action BeforeRender;
	static Action AdvanceFrame;

	static string Env(string k, string d) => Environment.GetEnvironmentVariable(k) ?? d;

	public static void Render()
	{
		string outDir = Env("WATER_OUT", "Temp/water");
		Directory.CreateDirectory(outDir);
		ShaderUtil.allowAsyncCompilation = false;

		EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
		foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
			go.SetActive(go.name == "Directional Light");

		var terrainSettings = AssetDatabase.LoadAssetAtPath<TerrainSettings>("Assets/ScriptableObjects/Game/Terrain/TerrainSettings.asset");
		float top = terrainSettings.TopHeight;
		float surface = top - 0.08f;
		float floor = top - 0.95f;
		BuildBank();
		BuildTerrain(terrainSettings);
		BuildPond(surface, floor, top);

		var rippleType = Type.GetType("Game.Tiles.WaterRippleSource, Lawnmower.Presentation");
		void Ripple(Vector3 position, float radius, float strength)
		{
			var go = new GameObject("Ripple");
			go.transform.position = position;
			var source = go.AddComponent(rippleType);
			rippleType.GetProperty("Radius").SetValue(source, radius);
			rippleType.GetProperty("Strength").SetValue(source, strength);
		}
		if (Env("WATER_SOURCES", "1") == "1")
		{
			Ripple(new Vector3(1.7f, surface, 1.9f), 0.3f, 0.2f);
			Ripple(new Vector3(-1.6f, surface, -0.5f), 0.3f, 0.1f);
			Ripple(new Vector3(0.9f, surface, 0.6f), 0.25f, 0.1f);
			Ripple(new Vector3(-1.0f, surface, -0.9f), 0.2f, 0.07f);
		}

		var waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Game/Tiles/Water/Water.mat");
		foreach (var message in ShaderUtil.GetShaderMessages(waterMaterial.shader))
			Debug.Log($"[water] shader {message.severity}: {message.message} line {message.line}");

		var water = GameObject.CreatePrimitive(PrimitiveType.Quad);
		Object.DestroyImmediate(water.GetComponent<Collider>());
		water.transform.SetPositionAndRotation(new Vector3(0, surface, 0.5f), Quaternion.Euler(90, 0, 0));
		// Half a tile bigger than the pond on every side so it tucks under the terrain's inset bank.
		water.transform.localScale = new Vector3(6, 5, 1);
		water.layer = 4;
		water.GetComponent<Renderer>().sharedMaterial = waterMaterial;

		var simulationType = Type.GetType("Game.Tiles.WaterRippleSimulation, Lawnmower.Presentation");
		var simulation = water.AddComponent(simulationType);
		simulationType.GetProperty("Size").SetValue(simulation, new Vector2(5, 4));
		var simulate = simulationType.GetMethod("Simulate");
		if (Env("WATER_DEBUG", "0") == "1")
		{
			float done = 0;
			foreach (float target in new[] { 2f, 4f, 8f, 16f })
			{
				simulate.Invoke(simulation, new object[] { target - done });
				done = target;
				DumpSimulation(outDir, $"t{target}");
			}
			return;
		}
		simulate.Invoke(simulation, new object[] { float.Parse(Env("WATER_WARMUP", "60")) });

		// Frog-style splashes during the clip, two simulation steps per 30 fps frame.
		var step = simulationType.GetMethod("Step");
		var splash = simulationType.GetMethod("Splash");
		var splashes = new Dictionary<int, Vector3>
		{
			{ 20, new Vector3(-0.2f, 0, 0.9f) },
			{ 75, new Vector3(1.3f, 0, -0.4f) },
			{ 130, new Vector3(-1.2f, 0, -0.1f) },
			{ 185, new Vector3(0.6f, 0, 0.3f) },
		};
		int frame = 0;
		AdvanceFrame = () =>
		{
			if (splashes.TryGetValue(frame++, out var position))
				splash.Invoke(simulation, new object[] { position, 0.08f, 1.5f });
			step.Invoke(simulation, null);
			step.Invoke(simulation, null);
		};

		var cam = new GameObject("Cam").AddComponent<Camera>();
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = new Color(0.589f, 0.823f, 0.823f);
		cam.fieldOfView = 7;
		cam.nearClipPlane = 1;
		cam.farClipPlane = 300;
		cam.transform.rotation = Quaternion.Euler(42.9f, 21.8f, 0);

		var reflectionType = Type.GetType("Game.Tiles.PlanarWaterReflection, Lawnmower.Presentation");
		var reflection = (MonoBehaviour)water.AddComponent(reflectionType);
		reflectionType.GetProperty("TargetCamera").SetValue(reflection, cam);
		var renderReflection = reflectionType.GetMethod("Render");
		if (Env("WATER_REFLECT", "1") == "1")
			BeforeRender = () => renderReflection.Invoke(reflection, null);
		else
			reflection.enabled = false;

		int w = int.Parse(Env("WATER_W", "1200")), h = w * 3 / 4;
		var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
		cam.targetTexture = rt;
		var tex = new Texture2D(w, h, TextureFormat.RGB24, false);

		var shots = new (string Name, Vector3 Target, float Distance)[]
		{
			("wide", new Vector3(0, surface, 0.5f), 78),
			("close", new Vector3(0.7f, surface, 1.2f), 38),
		};

		// The first frames after building the scene render with incomplete materials.
		cam.transform.position = shots[0].Target - cam.transform.forward * shots[0].Distance;
		for (int i = 0; i < 3; i++)
		{
			BeforeRender?.Invoke();
			cam.Render();
		}

		if (Env("WATER_VIDEO", "0") == "1")
		{
			RecordVideo(cam, rt, tex, shots[Env("WATER_SHOT", "close") == "wide" ? 0 : 1], Path.Combine(outDir, "Water.rgb"));
			return;
		}

		foreach (var shot in shots)
		{
			cam.transform.position = shot.Target - cam.transform.forward * shot.Distance;
			BeforeRender?.Invoke();
			cam.Render();
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
			tex.Apply();
			RenderTexture.active = null;
			File.WriteAllBytes(Path.Combine(outDir, $"Water_{shot.Name}.png"), tex.EncodeToPNG());
		}
		Debug.Log($"[water] wrote previews to {outDir}");
	}

	// URP feeds shaders the real clock outside play mode, so frames are paced to wall-clock time and kept in memory
	// until the clip is done. Output is raw bottom-up RGB24 for ffmpeg.
	static void RecordVideo(Camera cam, RenderTexture rt, Texture2D tex, (string Name, Vector3 Target, float Distance) shot, string path)
	{
		float fps = float.Parse(Env("WATER_FPS", "30")), seconds = float.Parse(Env("WATER_SECONDS", "6"));
		int frames = Mathf.RoundToInt(fps * seconds);
		cam.transform.position = shot.Target - cam.transform.forward * shot.Distance;
		var data = new List<byte[]>(frames);
		float maxLag = 0;
		float start = Time.realtimeSinceStartup + 0.1f;
		for (int f = 0; f < frames; f++)
		{
			float due = start + f / fps;
			while (Time.realtimeSinceStartup < due)
				System.Threading.Thread.Sleep(1);
			maxLag = Mathf.Max(maxLag, Time.realtimeSinceStartup - due);
			AdvanceFrame?.Invoke();
			BeforeRender?.Invoke();
			cam.Render();
			RenderTexture.active = rt;
			tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
			RenderTexture.active = null;
			data.Add(tex.GetRawTextureData());
		}
		using (var stream = File.Create(path))
			foreach (var frame in data)
				stream.Write(frame, 0, frame.Length);
		Debug.Log($"[water] {Path.GetFileName(path)}: {frames} frames {rt.width}x{rt.height}, max lag {maxLag * 1000:F0}ms");
	}

	static void DumpSimulation(string outDir, string tag)
	{
		var simTex = (RenderTexture)Shader.GetGlobalTexture("_WaterRippleSimTex");
		var read = new Texture2D(simTex.width, simTex.height, TextureFormat.RGBAFloat, false);
		RenderTexture.active = simTex;
		read.ReadPixels(new Rect(0, 0, simTex.width, simTex.height), 0, 0);
		RenderTexture.active = null;
		var px = read.GetPixels();
		float max = 0;
		foreach (var c in px) max = Mathf.Max(max, Mathf.Abs(c.r));
		Debug.Log($"[water] {tag} ripple height max {max}");
		for (int i = 0; i < px.Length; i++) { float v = px[i].r / Mathf.Max(max, 1e-5f) * 0.5f + 0.5f; px[i] = new Color(v, v, v, 1); }
		read.SetPixels(px);
		File.WriteAllBytes(Path.Combine(outDir, $"sim_{tag}.png"), read.EncodeToPNG());
	}

	const int MinX = -5, MaxX = 5, MinZ = -4, MaxZ = 6;

	static bool InPond(int x, int z) => x >= -2 && x <= 2 && z >= -1 && z <= 2;

	// The level's SDF terrain, as TerrainManager builds it, with the pond tiles left out like water tiles are.
	static void BuildTerrain(TerrainSettings settings)
	{
		int width = MaxX - MinX + 1, depth = MaxZ - MinZ + 1;
		var solid = new bool[width, depth];
		var cells = new Texture2D(width, depth, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
		for (int x = 0; x < width; x++)
		for (int z = 0; z < depth; z++)
		{
			solid[x, z] = !InPond(x + MinX, z + MinZ);
			cells.SetPixel(x, z, new Color(0.16f, 0.22f, 0.06f));
		}
		cells.Apply();

		var mesh = new Mesh { name = "Terrain" };
		var footprint = TerrainFootprint.Create(solid, settings.ConvexCornerRadius, settings.ConcaveCornerRadius, Allocator.TempJob);
		try
		{
			float margin = settings.Margin;
			TerrainMesher.Build(mesh, footprint, settings.Shape, new float3(-margin, settings.BottomHeight, -margin),
				new float3(width + margin, settings.TopHeight + settings.VoxelSize * 2, depth + margin), settings.VoxelSize);
		}
		finally
		{
			footprint.Dispose();
		}

		var terrain = new GameObject("Terrain");
		terrain.transform.position = new Vector3(MinX - 0.5f, 0, MinZ - 0.5f);
		terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
		var renderer = terrain.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles/Terrain.mat");
		var block = new MaterialPropertyBlock();
		block.SetTexture("_TerrainCells", cells);
		renderer.SetPropertyBlock(block);
	}

	static void BuildBank()
	{
		var grass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/Tiles/Grass/Grass.prefab");
		var stone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Game/Tiles/Stone/Stone.prefab");
		var setterType = Type.GetType("Game.Tiles.AppearanceSetter, Lawnmower.Presentation");
		Random.InitState(7);
		for (int x = MinX; x <= MaxX; x++)
		for (int z = MinZ; z <= MaxZ; z++)
		{
			if (InPond(x, z))
				continue;
			bool isStone = (x == 3 && z == 0) || (x == -3 && z == 2);
			var tile = (GameObject)PrefabUtility.InstantiatePrefab(isStone ? stone : grass);
			tile.transform.position = new Vector3(x, 0, z);
			if (!isStone)
			{
				var setter = tile.GetComponentInChildren(setterType, true);
				setterType.GetMethod("SetAppearance").Invoke(setter, new object[] { Random.Range(1, 4) });
			}
		}
	}

	static void BuildPond(float surface, float floor, float top)
	{
		var lit = Shader.Find("Universal Render Pipeline/Lit");
		var cache = new Dictionary<Color, Material>();
		Material Mat(Color c)
		{
			if (!cache.TryGetValue(c, out var m))
			{
				m = new Material(lit);
				m.SetColor("_BaseColor", c);
				m.SetFloat("_Smoothness", 0.25f);
				cache[c] = m;
			}
			return m;
		}
		GameObject Prim(PrimitiveType type, Vector3 pos, Vector3 scale, Quaternion rot, Color c)
		{
			var go = GameObject.CreatePrimitive(type);
			Object.DestroyImmediate(go.GetComponent<Collider>());
			go.transform.SetPositionAndRotation(pos, rot);
			go.transform.localScale = scale;
			go.GetComponent<Renderer>().sharedMaterial = Mat(c);
			return go;
		}

		Random.InitState(42);
		float depth = top - floor;
		var sand = new Color(0.95f, 0.88f, 0.7f);
		Prim(PrimitiveType.Cube, new Vector3(0, floor - 0.05f, 0.5f), new Vector3(6, 0.1f, 5), Quaternion.identity, sand);

		Color[] pebbleColours = { new(0.98f, 0.97f, 0.95f), new(0.85f, 0.8f, 0.78f), new(0.95f, 0.75f, 0.75f), new(0.7f, 0.78f, 0.85f) };
		for (int i = 0; i < 60; i++)
		{
			float s = Random.Range(0.06f, 0.14f);
			Prim(PrimitiveType.Sphere, new Vector3(Random.Range(-2.4f, 2.4f), floor, Random.Range(-1.4f, 2.4f)),
				new Vector3(s, s * 0.5f, s * 1.2f), Quaternion.Euler(0, Random.Range(0, 360f), 0), pebbleColours[i % pebbleColours.Length]);
		}

		Prim(PrimitiveType.Cube, new Vector3(1.55f, floor + 0.2f, -0.55f), new Vector3(1.6f, 0.12f, 1.4f), Quaternion.Euler(0, -10, -22), sand * 0.97f);

		var rock = new Color(0.58f, 0.62f, 0.72f);
		Prim(PrimitiveType.Sphere, new Vector3(-1.4f, floor + 0.1f, 1.6f), new Vector3(0.9f, 0.55f, 0.7f), Quaternion.Euler(0, 30, 10), rock);
		Prim(PrimitiveType.Sphere, new Vector3(-0.9f, floor + 0.05f, 1.9f), new Vector3(0.5f, 0.35f, 0.45f), Quaternion.Euler(0, 70, 0), rock * 0.9f);
		Prim(PrimitiveType.Sphere, new Vector3(1.7f, floor + 0.35f, 1.9f), new Vector3(0.9f, 1.4f, 0.8f), Quaternion.Euler(5, 20, -8), rock);
		Prim(PrimitiveType.Sphere, new Vector3(0.6f, floor + 0.05f, -0.9f), new Vector3(0.6f, 0.4f, 0.5f), Quaternion.Euler(0, -20, 0), rock * 1.05f);

		var plant = new Color(0.3f, 0.78f, 0.45f);
		void PlantClump(Vector3 basePos, int count, float height)
		{
			for (int i = 0; i < count; i++)
			{
				float hgt = height * Random.Range(0.6f, 1.1f);
				var rot = Quaternion.Euler(Random.Range(-14f, 14f), 0, Random.Range(-14f, 14f));
				var pos = basePos + new Vector3(Random.Range(-0.15f, 0.15f), 0, Random.Range(-0.15f, 0.15f));
				Prim(PrimitiveType.Capsule, pos + rot * Vector3.up * hgt * 0.5f, new Vector3(0.05f, hgt * 0.5f, 0.05f), rot, plant * Random.Range(0.85f, 1.1f));
			}
		}
		PlantClump(new Vector3(-2.1f, floor, -1.1f), 7, depth + 0.15f);
		PlantClump(new Vector3(2.1f, floor, 0.4f), 6, depth * 0.7f);
		PlantClump(new Vector3(-0.2f, floor, 2.2f), 5, depth * 0.8f);

		void Fish(Vector3 pos, float yaw, Color body, float size)
		{
			var rot = Quaternion.Euler(0, yaw, 0);
			Prim(PrimitiveType.Sphere, pos, new Vector3(0.13f, 0.09f, 0.36f) * size, rot, body);
			Prim(PrimitiveType.Cube, pos + rot * new Vector3(0, 0, -0.22f * size), new Vector3(0.12f, 0.02f, 0.12f) * size, rot * Quaternion.Euler(0, 45, 0), body);
			Prim(PrimitiveType.Sphere, pos + rot * new Vector3(0, 0.035f, 0.04f) * size, new Vector3(0.08f, 0.04f, 0.14f) * size, rot, Color.white);
		}
		Fish(new Vector3(-0.6f, surface - 0.2f, 0.3f), 60, new Color(1f, 0.45f, 0.12f), 1.1f);
		Fish(new Vector3(0.4f, surface - 0.45f, 1.1f), -30, new Color(1f, 0.8f, 0.2f), 1f);
		Fish(new Vector3(1.2f, surface - 0.3f, -0.4f), 150, new Color(0.95f, 0.95f, 0.95f), 0.9f);
		Fish(new Vector3(-1.5f, surface - 0.6f, -0.6f), 200, new Color(1f, 0.35f, 0.3f), 1.2f);

		var pad = new Color(0.36f, 0.72f, 0.3f);
		Prim(PrimitiveType.Cylinder, new Vector3(-1.6f, surface, -0.5f), new Vector3(0.6f, 0.012f, 0.6f), Quaternion.identity, pad);
		Prim(PrimitiveType.Cylinder, new Vector3(-1.0f, surface, -0.9f), new Vector3(0.4f, 0.012f, 0.4f), Quaternion.identity, pad * 1.08f);
		Prim(PrimitiveType.Cylinder, new Vector3(0.9f, surface, 0.6f), new Vector3(0.5f, 0.012f, 0.5f), Quaternion.identity, pad * 0.95f);
		var petal = new Color(1f, 0.72f, 0.85f);
		for (int i = 0; i < 6; i++)
		{
			var rot = Quaternion.Euler(-35, i * 60, 0);
			Prim(PrimitiveType.Sphere, new Vector3(-1.6f, surface + 0.06f, -0.5f) + rot * new Vector3(0, 0, 0.07f), new Vector3(0.06f, 0.03f, 0.13f), rot, petal);
		}
		Prim(PrimitiveType.Sphere, new Vector3(-1.6f, surface + 0.08f, -0.5f), Vector3.one * 0.06f, Quaternion.identity, new Color(1f, 0.92f, 0.4f));
	}
}
