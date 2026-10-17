using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>
	/// Captures a level's board, plus a close-up of its shoreline edging, for judging the shore rocks by eye.
	/// <para>From a shell (not batch mode; the board camera sizes itself from the Game view):</para>
	/// <para>Unity -projectPath . -executeMethod Lawnmower.Editor.GameScreens.ShoreCapture.Run</para>
	/// Environment: SHORE_OUT (output folder, default Temp/shore), SHORE_LEVELS (comma-separated 0-based indices, default 0,6).
	/// </summary>
	[InitializeOnLoad]
	public static class ShoreCapture
	{
		private const string StepKey = "ShoreCapture.Step";
		private const string ExitKey = "ShoreCapture.Exit";
		private const int Width = 1170, Height = 2532;

		private static double _wait;

		private static string OutDir => Environment.GetEnvironmentVariable("SHORE_OUT") ?? "Temp/shore";

		private static int[] Levels => Array.ConvertAll((Environment.GetEnvironmentVariable("SHORE_LEVELS") ?? "0,6").Split(','), int.Parse);

		static ShoreCapture()
		{
			if (SessionState.GetInt(StepKey, 0) > 0)
			{
				EditorApplication.update += Tick;
			}
		}

		public static void Run()
		{
			Directory.CreateDirectory(OutDir);
			Lawnmower.Screens.Editor.GameViewResolution.Set(390, 844);
			SessionState.SetBool(ExitKey, Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") >= 0);
			EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
			SessionState.SetInt(StepKey, 1);
			EditorApplication.update -= Tick;
			EditorApplication.update += Tick;
			EditorApplication.EnterPlaymode();
		}

		private static void Tick()
		{
			int step = SessionState.GetInt(StepKey, 0);
			if (step == 0 || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < _wait)
			{
				return;
			}

			try
			{
				int[] levels = Levels;
				if (step == 1)
				{
					Debug.Log($"[ShoreCapture] {GameScreensDebug.LoadLevel(levels[0])}");
					Next(2, 7);
					return;
				}

				int index = step - 2;
				CaptureShots($"level{levels[index] + 1}");
				if (index + 1 >= levels.Length)
				{
					Finish(0);
					return;
				}

				Debug.Log($"[ShoreCapture] {GameScreensDebug.LoadLevel(levels[index + 1])}");
				Next(step + 1, 7);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				Finish(1);
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

		private static void CaptureShots(string name)
		{
			Camera camera = Camera.main;
			Save(Render(camera, Width, Height), $"{name}-board.png");

			// A close-up: the same view, narrowed onto the nearest corner of the shoreline
			MeshFilter edgingFilter = Array.Find(UnityEngine.Object.FindObjectsByType<MeshFilter>(),
				f => f.sharedMesh != null && f.sharedMesh.name == "TerrainEdging");
			if (edgingFilter == null)
			{
				Debug.LogWarning("[ShoreCapture] no TerrainEdging in the scene");
				return;
			}

			Renderer edging = edgingFilter.GetComponent<Renderer>();

			Bounds bounds = edging.bounds;
			Vector3 toCamera = -camera.transform.forward;
			Vector3 corner = new Vector3(
				toCamera.x > 0 ? bounds.max.x : bounds.min.x,
				bounds.center.y,
				toCamera.z > 0 ? bounds.max.z : bounds.min.z);
			Vector3 target = Vector3.Lerp(bounds.center, corner, 0.55f);

			GameObject closeObject = new GameObject("ShoreCloseUp");
			Camera close = closeObject.AddComponent<Camera>();
			close.CopyFrom(camera);
			close.transform.rotation = camera.transform.rotation;
			float distance = Vector3.Distance(camera.transform.position, bounds.center);
			close.transform.position = target - close.transform.forward * distance;
			close.fieldOfView = camera.fieldOfView * 0.38f;
			Save(Render(close, 1600, 1200), $"{name}-close.png");
			UnityEngine.Object.DestroyImmediate(closeObject);
		}

		private static Texture2D Render(Camera camera, int width, int height)
		{
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
			return texture;
		}

		private static void Save(Texture2D texture, string file)
		{
			string path = Path.Combine(OutDir, file);
			File.WriteAllBytes(path, texture.EncodeToPNG());
			Debug.Log($"[ShoreCapture] wrote {path}");
		}
	}
}
