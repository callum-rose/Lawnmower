using System;
using System.IO;
using MainMenu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>
	/// Captures the 3D layers under the UI (the board in a few states, the main-menu hero bunny) for the HTML UX prototype
	/// in docs/ux.html. Only the cameras are rendered, so the overlay UI isn't in the shots.
	/// <para>Menu: Bunny Heaven > Screens > Capture UX Prototype Backdrops, or from a shell (not batch mode):</para>
	/// <para>Unity -projectPath . -executeMethod Lawnmower.Editor.GameScreens.UxCapture.Run</para>
	/// </summary>
	[InitializeOnLoad]
	public static class UxCapture
	{
		private const string StepKey = "UxCapture.Step";
		private const string ExitKey = "UxCapture.Exit";
		private const string OutDir = "docs/ux";
		private const int Width = 1170, Height = 2532;

		private static double _wait;

		static UxCapture()
		{
			if (SessionState.GetInt(StepKey, 0) > 0)
			{
				EditorApplication.update += Tick;
			}
		}

		[MenuItem("Bunny Heaven/Screens/Capture UX Prototype Backdrops")]
		public static void Run()
		{
			Directory.CreateDirectory(OutDir);

			// The board camera and the hero image size themselves from the screen, so it has to be the 390x844 frame:
			// batch mode's screen is a fixed 640x480, so this runs in a windowed editor with the Game view set.
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
				switch (step)
				{
					case 1: Next(2, 2); break;
					case 2:
						Debug.Log($"[UxCapture] Screen {Screen.width}x{Screen.height}, batch {Application.isBatchMode}");
						CaptureHeroBunny("menu-bunny.png");
						GameScreensDebug.LoadLevel(0);
						Next(3, 3);
						break;
					case 3:
						CaptureBoard("game-level1.jpg");
						GameScreensDebug.LoadLevel(6);
						Next(4, 3);
						break;
					case 4:
						CaptureBoard("game-level7.jpg");
						Debug.Log($"[UxCapture] fail: {GameScreensDebug.ForceFail()}");
						Next(5, 2.5);
						break;
					case 5:
						CaptureBoard("game-failed.jpg");
						GameScreensDebug.LoadLevel(6);
						Next(6, 3);
						break;
					case 6:
						Debug.Log($"[UxCapture] solve: {GameScreensDebug.PlaySolution()}");
						Next(7, 4);
						break;
					case 7:
						CaptureBoard("game-complete.jpg");
						Finish(0);
						break;
				}
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

		private static void CaptureBoard(string file)
		{
			Camera camera = Camera.main;
			Save(Render(camera, Width, Height, null), file);
		}

		/// <summary>The hero bunny's own camera, on a transparent background so the page's backdrop shows through.</summary>
		private static void CaptureHeroBunny(string file)
		{
			CameraRawImage hero = UnityEngine.Object.FindAnyObjectByType<CameraRawImage>();
			Rect rect = ((RectTransform)hero.transform).rect;
			int height = Mathf.RoundToInt(Width * rect.height / rect.width);
			Save(Render(hero.SourceCamera, Width, height, Color.clear), file);
		}

		private static Texture2D Render(Camera camera, int width, int height, Color? clear)
		{
			RenderTexture previousTarget = camera.targetTexture;
			CameraClearFlags previousFlags = camera.clearFlags;
			Color previousColor = camera.backgroundColor;
			float previousAspect = camera.aspect;

			var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
			camera.targetTexture = rt;
			camera.aspect = (float)width / height;
			if (clear.HasValue)
			{
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = clear.Value;
			}

			camera.Render();

			RenderTexture.active = rt;
			var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
			texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			texture.Apply();
			RenderTexture.active = null;

			camera.targetTexture = previousTarget;
			camera.clearFlags = previousFlags;
			camera.backgroundColor = previousColor;
			camera.aspect = previousAspect;
			rt.Release();
			return texture;
		}

		private static void Save(Texture2D texture, string file)
		{
			string path = Path.Combine(OutDir, file);
			File.WriteAllBytes(path, file.EndsWith(".jpg") ? texture.EncodeToJPG(85) : texture.EncodeToPNG());
			Debug.Log($"[UxCapture] wrote {path}");
		}
	}
}
