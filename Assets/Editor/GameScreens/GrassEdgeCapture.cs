using System;
using System.IO;
using System.Reflection;
using Game.Core;
using Game.Tiles;
using Lawnmower.Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>
	/// Captures a few boards for judging the short blades along grass tiles' edges where they meet dirt: each level as
	/// loaded (the bare shore around it), then with a couple of inner tiles mowed bare, with a close-up of them.
	/// Runs in play mode; from a shell, in batch mode without -quit:
	/// <para>Unity -batchmode -projectPath "$PWD" -executeMethod Lawnmower.Editor.GameScreens.GrassEdgeCapture.Run</para>
	/// Environment variables: EDGE_OUT (output folder, default Captures/grass-edges), EDGE_W / EDGE_H (image size),
	/// EDGE_HIDE=1 (hide the edge blades, for a before shot).
	/// </summary>
	[InitializeOnLoad]
	public static class GrassEdgeCapture
	{
		private const string StepKey = "GrassEdgeCapture.Step";
		private const string ExitKey = "GrassEdgeCapture.Exit";

		private static readonly (int level, string name)[] Shots =
		{
			(2, "level03"),
			(6, "level07"),
		};

		private static double _wait;

		private static string OutDir => Environment.GetEnvironmentVariable("EDGE_OUT") ?? "Captures/grass-edges";

		static GrassEdgeCapture()
		{
			if (SessionState.GetInt(StepKey, 0) > 0)
			{
				EditorApplication.update += Tick;
			}
		}

		[MenuItem("Bunny Heaven/Screens/Capture Grass Edges")]
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

		// Each shot takes three steps: load its level, capture it then mow some of it bare, then capture that
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

				string name = Shots[shot].name;
				switch ((step - 1) % 3)
				{
					case 0:
						Debug.Log($"[GrassEdgeCapture] {GameScreensDebug.LoadLevel(Shots[shot].level)}");
						// Long enough for the level's intro, which runs slowly in batch mode
						Next(step + 1, 15);
						break;
					case 1:
						Capture(name + ".png");
						Next(step + 1, MowMiddleBare() ? 4 : 0);
						break;
					default:
						Capture(name + "-bare.png");
						CaptureMiddle(name + "-bare-closeup.png");
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

		/// <summary>Sets two neighbouring grass tiles off the board's edge and away from the mower to bare dirt in the model,
		/// as mowing them would.</summary>
		private static bool MowMiddleBare()
		{
			Level level = GameScreensDebug.Game.Simulation.Level;
			bool mowed = false;
			foreach (GridVector position in new[] { BareTile(level), BareTile(level) + GridVector.Right })
			{
				if (level.GetTile(position) is GrassTile grass)
				{
					FieldInfo field = typeof(GrassTile).GetField("_internalGrassHeight", BindingFlags.Instance | BindingFlags.NonPublic);
					object property = field.GetValue(grass);
					MethodInfo setValue = property.GetType().GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					Type xor = setValue.GetParameters()[1].ParameterType;
					setValue.Invoke(property, new[] { 0, Activator.CreateInstance(xor) });
					mowed = true;
				}
			}

			return mowed;
		}

		// The inner grass tile furthest from where the mower starts, so the mower isn't standing in the way
		private static GridVector BareTile(Level level)
		{
			GridVector best = new GridVector(level.Width / 2, level.Depth / 2);
			double bestDistance = -1;
			for (int z = 1; z < level.Depth - 1; z++)
			{
				for (int x = 1; x < level.Width - 2; x++)
				{
					GridVector position = new GridVector(x, z);
					double distance = (position - level.StartPosition).Magnitude;
					if (level.GetTile(position) is GrassTile && distance > bestDistance)
					{
						best = position;
						bestDistance = distance;
					}
				}
			}

			return best;
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

		/// <summary>The game camera zoomed in on the mowed-bare tiles, then put back.</summary>
		private static void CaptureMiddle(string file)
		{
			Level level = GameScreensDebug.Game.Simulation.Level;
			GrassTileObject tile = null;
			foreach (GrassTileObject candidate in UnityEngine.Object.FindObjectsByType<GrassTileObject>())
			{
				if (candidate.name.Contains(BareTile(level).ToString()))
				{
					tile = candidate;
				}
			}

			if (tile == null)
			{
				return;
			}

			Vector3 target = tile.transform.position + new Vector3(0.5f, 0, 0);
			Camera camera = Camera.main;
			Quaternion rotation = camera.transform.rotation;
			float fov = camera.fieldOfView;
			float size = camera.orthographicSize;
			float distance = Vector3.Distance(camera.transform.position, target);
			camera.transform.LookAt(target);
			camera.fieldOfView = 2 * Mathf.Atan(1.6f / distance) * Mathf.Rad2Deg;
			camera.orthographicSize = 1.6f;
			Capture(file);
			camera.transform.rotation = rotation;
			camera.fieldOfView = fov;
			camera.orthographicSize = size;
		}

		private static void Capture(string file)
		{
			// For a before shot to compare against
			if (Environment.GetEnvironmentVariable("EDGE_HIDE") == "1")
			{
				foreach (MeshFilter filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
				{
					if (filter.name.StartsWith("Edge "))
					{
						filter.gameObject.SetActive(false);
					}
				}

				file = Path.GetFileNameWithoutExtension(file) + "-before.png";
			}

			int width = int.Parse(Environment.GetEnvironmentVariable("EDGE_W") ?? "1448");
			int height = int.Parse(Environment.GetEnvironmentVariable("EDGE_H") ?? "1086");
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
			Debug.Log($"[GrassEdgeCapture] wrote {path}");
		}
	}
}
