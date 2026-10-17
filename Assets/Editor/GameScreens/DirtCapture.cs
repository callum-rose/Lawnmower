using System;
using System.IO;
using System.Reflection;
using Game.Terrain;
using Game.Tiles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>
	/// Captures the board from the game camera for judging the raked dirt: a few levels with every grass tile shown as
	/// dirt, as if mowed bare, each with a close-up of where a rake path ends. Runs in play mode; from a shell, in batch mode without -quit:
	/// <para>Unity -batchmode -projectPath . -executeMethod Lawnmower.Editor.GameScreens.DirtCapture.Run</para>
	/// Environment variables: DIRT_OUT (output folder, default Temp/dirt), DIRT_W / DIRT_H (image size).
	/// </summary>
	[InitializeOnLoad]
	public static class DirtCapture
	{
		private const string StepKey = "DirtCapture.Step";
		private const string ExitKey = "DirtCapture.Exit";

		private static readonly (int level, string file)[] Shots =
		{
			(2, "level03-dirt.png"),
			(6, "level07-dirt.png"),
			(9, "level10-dirt.png"),
		};

		private static double _wait;

		private static string OutDir => Environment.GetEnvironmentVariable("DIRT_OUT") ?? "Temp/dirt";

		static DirtCapture()
		{
			if (SessionState.GetInt(StepKey, 0) > 0)
			{
				EditorApplication.update += Tick;
			}
		}

		[MenuItem("Bunny Heaven/Screens/Capture Raked Dirt")]
		public static void Run()
		{
			Directory.CreateDirectory(OutDir);
			SessionState.SetBool(ExitKey, Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") >= 0);
			EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
			SessionState.SetInt(StepKey, 1);
			EditorApplication.update -= Tick;
			EditorApplication.update += Tick;
			EditorApplication.EnterPlaymode();
		}

		// Each shot takes three steps: load its level, show it as dirt, then capture it once that has been drawn
		private static void Tick()
		{
			int step = SessionState.GetInt(StepKey, 0);
			if (step == 0 || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _wait)
			{
				return;
			}

			try
			{
				int shot = (step - 1) / 3;
				if (shot >= Shots.Length)
				{
					Finish(0);
					return;
				}

				switch ((step - 1) % 3)
				{
					case 0:
						Debug.Log($"[DirtCapture] {GameScreensDebug.LoadLevel(Shots[shot].level)}");
						// Long enough for the level's intro, which runs slowly in batch mode
						Next(step + 1, 8);
						break;
					case 1:
						ShowAllDirt();
						Next(step + 1, 0.5);
						break;
					default:
						Capture(Shots[shot].file);
						CapturePathEnd(Path.GetFileNameWithoutExtension(Shots[shot].file) + "-end.png");
						Next(step + 1, 0.5);
						break;
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Finish(1);
			}
		}

		/// <summary>Every grass tile drawn mowed bare, without touching the level's state.</summary>
		private static void ShowAllDirt()
		{
			foreach (AppearanceSetter setter in UnityEngine.Object.FindObjectsByType<AppearanceSetter>())
			{
				setter.SetAppearance(0);
			}
		}

		private static void Next(int step, double seconds)
		{
			SessionState.SetInt(StepKey, step);
			_wait = EditorApplication.timeSinceStartup + seconds;
		}

		private static void Finish(int code)
		{
			SessionState.EraseInt(StepKey);
			EditorApplication.update -= Tick;
			EditorApplication.ExitPlaymode();
			if (SessionState.GetBool(ExitKey, false))
			{
				EditorApplication.Exit(code);
			}
		}

		/// <summary>The game camera zoomed in on the first tile a rake path ends on, then put back.</summary>
		private static void CapturePathEnd(string file)
		{
			TerrainManager terrain = UnityEngine.Object.FindAnyObjectByType<TerrainManager>();
			Texture2D rake = (Texture2D)typeof(TerrainManager)
				.GetField("_rake", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(terrain);
			Color32[] routes = rake.GetPixels32();
			int end = Array.FindIndex(routes, r => (r.r == RakePath.None) != (r.g == RakePath.None));
			if (end < 0)
			{
				return;
			}

			Vector3 tile = terrain.transform.TransformPoint(end % rake.width + 0.5f, 0, end / rake.width + 0.5f);
			tile.y = terrain.GetComponent<MeshRenderer>().bounds.max.y;
			// Zoomed rather than moved in, which could put the board inside the near clip plane
			Camera camera = Camera.main;
			Quaternion rotation = camera.transform.rotation;
			float fov = camera.fieldOfView;
			float size = camera.orthographicSize;
			float tileSize = terrain.transform.lossyScale.x;
			float distance = Vector3.Distance(camera.transform.position, tile);
			camera.transform.LookAt(tile);
			camera.fieldOfView = 2 * Mathf.Atan(tileSize * 1.2f / distance) * Mathf.Rad2Deg;
			camera.orthographicSize = tileSize * 1.2f;
			Capture(file);
			camera.transform.rotation = rotation;
			camera.fieldOfView = fov;
			camera.orthographicSize = size;
		}

		private static void Capture(string file)
		{
			int width = int.Parse(Environment.GetEnvironmentVariable("DIRT_W") ?? "1448");
			int height = int.Parse(Environment.GetEnvironmentVariable("DIRT_H") ?? "1086");
			Camera camera = Camera.main;
			RenderTexture previousTarget = camera.targetTexture;
			float previousAspect = camera.aspect;

			var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
			camera.targetTexture = rt;
			camera.aspect = (float)width / height;
			camera.Render();

			RenderTexture.active = rt;
			var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
			texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			texture.Apply();
			RenderTexture.active = null;

			camera.targetTexture = previousTarget;
			camera.aspect = previousAspect;
			rt.Release();

			string path = Path.Combine(OutDir, file);
			File.WriteAllBytes(path, texture.EncodeToPNG());
			Debug.Log($"[DirtCapture] wrote {path}");
		}
	}
}
