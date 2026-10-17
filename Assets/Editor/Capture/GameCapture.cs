using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Lawnmower.Editor.Capture
{
	/// <summary>
	/// One screenshot command for agents, the same in a live Editor and in batch mode:
	/// <para>Live:  unity command capture --scene Assets/Scenes/Game.unity --output Captures/game.png</para>
	/// <para>Batch: unity run . --command capture -- --scene Assets/Scenes/Game.unity --output Captures/game.png</para>
	/// Renders a camera into a texture of the requested size instead of reading the Game view, which batch mode doesn't
	/// have. Screen Space - Overlay canvases never render through a camera, so for the shot they're moved onto the
	/// capture camera (Screen Space - Camera) and put back afterwards.
	/// </summary>
	public static class GameCapture
	{
		public const string DefaultFolder = "Captures";
		private const int MaxDimension = 8192;

		public sealed class Result
		{
			public string Path;
			public int Width;
			public int Height;
			public string Scene;
			public string Camera;
			public int OverlayCanvases;
			public bool PlayMode;
			public bool BatchMode;
			public string Setup;
		}

		[CliCommand("capture",
			"Render a game camera to a PNG/JPG at any size, in a live Editor or batch mode. Opens --scene (edit mode only; " +
			"a live Editor's scenes are restored afterwards), runs an optional static --setup method, then renders --camera " +
			"with Screen Space - Overlay UI included. In Play Mode it captures the running game as is.",
			Tags = new[] { "capture" })]
		public static Result Capture(
			[CliArg("scene", "Scene to open first, e.g. Assets/Scenes/Game.unity. Edit mode only. Default: the open scene(s).")]
			string scene = "",
			[CliArg("camera", "Camera GameObject name (disabled cameras count). Default: Camera.main, else the first camera.")]
			string camera = "",
			[CliArg("width", "Output width in pixels (default 1170, a portrait phone).")]
			int width = 1170,
			[CliArg("height", "Output height in pixels (default 2532).")]
			int height = 2532,
			[CliArg("ui", "Include Screen Space - Overlay canvases (default true).")]
			bool ui = true,
			[CliArg("warmup", "Frames to render and throw away before the kept one, to settle temporal effects and UI layout (default 2).")]
			int warmup = 2,
			[CliArg("setup", "Static method to run after the scene opens and before rendering, as Namespace.Type.Method. " +
				"It may take no parameters or one string (--setup_arg); its return value is reported.")]
			string setup = "",
			[CliArg("setup_arg", "String passed to the --setup method when it takes one.")]
			string setupArg = "",
			[CliArg("output", "Output file, absolute or relative to the project; .jpg/.jpeg writes a JPG. " +
				"Default: Captures/<scene>-<camera>-<time>.png. Not Temp/: Unity deletes it on exit")]
			string output = "")
		{
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				throw new InvalidOperationException(
					"No graphics device: the Editor was started with -nographics, so nothing can render. Run without it.");
			}

			width = Mathf.Clamp(width, 1, MaxDimension);
			height = Mathf.Clamp(height, 1, MaxDimension);

			bool playing = EditorApplication.isPlaying;
			SceneSetup[] restore = null;
			if (!string.IsNullOrEmpty(scene))
			{
				if (playing)
				{
					throw new InvalidOperationException("--scene can't be used in Play Mode; it captures the running game.");
				}

				restore = OpenScene(scene);
			}

			try
			{
				string setupResult = string.IsNullOrEmpty(setup) ? null : RunSetup(setup, setupArg);

				Camera cam = FindCamera(camera);
				Texture2D texture = Render(cam, width, height, ui, Mathf.Max(0, warmup), out int overlays);

				string path = ResolveOutput(output, cam);
				Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
				string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
				File.WriteAllBytes(path,
					extension == ".jpg" || extension == ".jpeg" ? texture.EncodeToJPG(90) : texture.EncodeToPNG());
				Object.DestroyImmediate(texture);

				var result = new Result
				{
					Path = path,
					Width = width,
					Height = height,
					Scene = string.Join(",", OpenScenePaths()),
					Camera = cam.name,
					OverlayCanvases = overlays,
					PlayMode = playing,
					BatchMode = Application.isBatchMode,
					Setup = setupResult,
				};
				Debug.Log($"[GameCapture] wrote {path} ({width}x{height}, camera {cam.name}, {overlays} overlay canvases)");
				return result;
			}
			finally
			{
				if (restore != null && restore.Length > 0 && !Application.isBatchMode)
				{
					EditorSceneManager.RestoreSceneManagerSetup(restore);
				}
			}
		}

		/// <summary>Opens <paramref name="scenePath"/> alone, returning the previous setup to restore, or null if it was
		/// already the only scene open.</summary>
		private static SceneSetup[] OpenScene(string scenePath)
		{
			if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
			{
				throw new ArgumentException($"No scene at '{scenePath}'.");
			}

			if (EditorSceneManager.sceneCount == 1 && EditorSceneManager.GetActiveScene().path == scenePath)
			{
				return null;
			}

			var dirty = new List<string>();
			for (int i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				var s = EditorSceneManager.GetSceneAt(i);
				if (s.isDirty)
				{
					dirty.Add(string.IsNullOrEmpty(s.path) ? s.name : s.path);
				}
			}

			if (dirty.Count > 0)
			{
				throw new InvalidOperationException(
					$"Unsaved changes in {string.Join(", ", dirty)}; save or discard them before capturing another scene.");
			}

			SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
			return previous;
		}

		private static string RunSetup(string setup, string arg)
		{
			int dot = setup.LastIndexOf('.');
			if (dot <= 0)
			{
				throw new ArgumentException($"--setup '{setup}' must be Namespace.Type.Method.");
			}

			string typeName = setup.Substring(0, dot);
			string methodName = setup.Substring(dot + 1);
			Type type = AppDomain.CurrentDomain.GetAssemblies()
				.Select(a => a.GetType(typeName, false))
				.FirstOrDefault(t => t != null)
				?? throw new ArgumentException($"--setup type '{typeName}' not found.");

			const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			MethodInfo method =
				type.GetMethod(methodName, flags, null, new[] { typeof(string) }, null)
				?? type.GetMethod(methodName, flags, null, Type.EmptyTypes, null)
				?? throw new ArgumentException($"--setup method '{methodName}' on {typeName} must be static and take no " +
					"parameters or one string.");

			object value;
			try
			{
				value = method.Invoke(null, method.GetParameters().Length == 1 ? new object[] { arg } : null);
			}
			catch (TargetInvocationException e)
			{
				throw new InvalidOperationException($"--setup {setup} threw: {e.InnerException?.Message}", e.InnerException);
			}

			return value?.ToString() ?? "ok";
		}

		private static Camera FindCamera(string name)
		{
			Camera[] all = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			if (!string.IsNullOrEmpty(name))
			{
				return all.FirstOrDefault(c => c.name == name)
					?? throw new ArgumentException(
						$"No camera named '{name}'. Cameras: {string.Join(", ", all.Select(c => c.name).Distinct())}");
			}

			return Camera.main
				?? all.FirstOrDefault(c => c.isActiveAndEnabled)
				?? all.FirstOrDefault()
				?? throw new InvalidOperationException("The open scene(s) have no camera.");
		}

		private static Texture2D Render(Camera cam, int width, int height, bool ui, int warmup, out int overlays)
		{
			var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
			RenderTexture previousTarget = cam.targetTexture;
			RenderTexture previousActive = RenderTexture.active;
			int previousMask = cam.cullingMask;
			List<(Canvas canvas, float planeDistance)> moved = ui ? MoveOverlaysOnto(cam) : new List<(Canvas, float)>();
			overlays = moved.Count;

			try
			{
				cam.targetTexture = rt;
				cam.ResetAspect();
				for (int i = 0; i <= warmup; i++)
				{
					Canvas.ForceUpdateCanvases();
					cam.Render();
				}

				RenderTexture.active = rt;
				var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
				texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				texture.Apply();
				return texture;
			}
			finally
			{
				foreach ((Canvas canvas, float planeDistance) in moved)
				{
					canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					canvas.worldCamera = null;
					canvas.planeDistance = planeDistance;
				}

				cam.cullingMask = previousMask;
				cam.targetTexture = previousTarget;
				cam.ResetAspect();
				RenderTexture.active = previousActive;
				rt.Release();
				Object.DestroyImmediate(rt);
			}
		}

		/// <summary>Puts every active root Screen Space - Overlay canvas just in front of <paramref name="cam"/>, and adds
		/// their layers to its culling mask (a UI camera's mask is often Nothing, as overlays don't need it).</summary>
		private static List<(Canvas, float)> MoveOverlaysOnto(Camera cam)
		{
			var moved = new List<(Canvas, float)>();
			float distance = cam.nearClipPlane + Mathf.Min(0.1f, (cam.farClipPlane - cam.nearClipPlane) * 0.01f);
			foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
				{
					continue;
				}

				moved.Add((canvas, canvas.planeDistance));
				canvas.renderMode = RenderMode.ScreenSpaceCamera;
				canvas.worldCamera = cam;
				canvas.planeDistance = distance;
				foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
				{
					cam.cullingMask |= 1 << t.gameObject.layer;
				}
			}

			return moved;
		}

		private static string ResolveOutput(string output, Camera cam)
		{
			string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
			if (string.IsNullOrEmpty(output))
			{
				string sceneName = cam.gameObject.scene.IsValid() ? cam.gameObject.scene.name : "scene";
				output = $"{DefaultFolder}/{sceneName}-{cam.name}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png";
			}

			return System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(output)
				? output
				: System.IO.Path.Combine(projectRoot, output));
		}

		private static IEnumerable<string> OpenScenePaths()
		{
			for (int i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				yield return EditorSceneManager.GetSceneAt(i).path;
			}
		}
	}
}
