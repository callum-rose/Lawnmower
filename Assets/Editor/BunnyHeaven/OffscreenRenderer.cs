using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Lawnmower.Editor.BunnyHeaven
{
	/// <summary>
	/// Renders objects in an isolated preview scene (no open scene is touched) and returns
	/// straight-alpha pixels, plus small CPU image helpers shared by the icon and preview renderers.
	/// </summary>
	internal sealed class OffscreenRenderer : IDisposable
	{
		private readonly Scene _scene;

		public Camera Camera { get; }

		public OffscreenRenderer()
		{
			_scene = EditorSceneManager.NewPreviewScene();

			Camera = new GameObject("Preview Camera").AddComponent<Camera>();
			Camera.clearFlags = CameraClearFlags.SolidColor;
			Camera.cameraType = CameraType.Game;
			Camera.allowHDR = false;
			Camera.allowMSAA = true;
			Camera.scene = _scene;
			Camera.enabled = false;
			Add(Camera.gameObject);

			Light key = new GameObject("Key Light").AddComponent<Light>();
			key.type = LightType.Directional;
			key.intensity = 1.1f;
			key.shadows = LightShadows.Soft;
			key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
			Add(key.gameObject);

			Light fill = new GameObject("Fill Light").AddComponent<Light>();
			fill.type = LightType.Directional;
			fill.intensity = 0.45f;
			fill.shadows = LightShadows.None;
			fill.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
			Add(fill.gameObject);
		}

		public void Add(GameObject gameObject)
		{
			SceneManager.MoveGameObjectToScene(gameObject, _scene);
		}

		public GameObject Instantiate(GameObject prefab)
		{
			GameObject instance = Object.Instantiate(prefab);
			instance.name = prefab.name;
			Add(instance);
			return instance;
		}

		/// <summary>
		/// Renders twice (black and white backgrounds) and recovers coverage from the difference, which
		/// works whatever the render pipeline does to the alpha channel. Returns straight (un-premultiplied) RGBA.
		/// </summary>
		public Color[] RenderStraightAlpha(int width, int height)
		{
			Color[] onBlack = RenderOnce(width, height, Color.black);
			Color[] onWhite = RenderOnce(width, height, Color.white);

			Color[] result = new Color[onBlack.Length];
			for (int i = 0; i < result.Length; i++)
			{
				Color b = onBlack[i];
				Color w = onWhite[i];
				float alpha = 1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f;
				alpha = Mathf.Clamp01(alpha);
				result[i] = alpha > 0.001f
					? new Color(Mathf.Clamp01(b.r / alpha), Mathf.Clamp01(b.g / alpha), Mathf.Clamp01(b.b / alpha), alpha)
					: Color.clear;
			}

			return result;
		}

		private Color[] RenderOnce(int width, int height, Color background)
		{
			RenderTexture rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
			{
				sRGB = true,
				msaaSamples = 8,
			});
			RenderTexture previous = RenderTexture.active;

			try
			{
				Camera.backgroundColor = background;
				Camera.targetTexture = rt;
				Camera.Render();

				RenderTexture.active = rt;
				Texture2D readback = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
				readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
				readback.Apply();
				Color[] pixels = readback.GetPixels();
				Object.DestroyImmediate(readback);
				return pixels;
			}
			finally
			{
				Camera.targetTexture = null;
				RenderTexture.active = previous;
				RenderTexture.ReleaseTemporary(rt);
			}
		}

		public void Dispose()
		{
			EditorSceneManager.ClosePreviewScene(_scene);
		}

		#region Image helpers

		/// <summary>Bounds (inclusive) of pixels with alpha above <paramref name="threshold"/>.</summary>
		public static RectInt OpaqueBounds(Color[] pixels, int width, int height, float threshold = 0.02f)
		{
			int minX = width, minY = height, maxX = -1, maxY = -1;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (pixels[x + y * width].a <= threshold)
					{
						continue;
					}

					minX = Mathf.Min(minX, x);
					maxX = Mathf.Max(maxX, x);
					minY = Mathf.Min(minY, y);
					maxY = Mathf.Max(maxY, y);
				}
			}

			return maxX < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
		}

		/// <summary>
		/// Alpha mask of everything within <paramref name="radius"/> pixels of a covered pixel, antialiased,
		/// via an exact Euclidean distance transform (linear time, so 1024px icons stay fast).
		/// </summary>
		public static float[] Dilate(Color[] pixels, int width, int height, float radius)
		{
			const float inf = 1e20f;
			float[] grid = new float[pixels.Length];
			for (int i = 0; i < grid.Length; i++)
			{
				grid[i] = pixels[i].a >= 0.5f ? 0f : inf;
			}

			int n = Mathf.Max(width, height);
			float[] f = new float[n];
			float[] d = new float[n];
			int[] v = new int[n];
			float[] z = new float[n + 1];

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++) f[y] = grid[x + y * width];
				DistanceTransform1D(f, d, v, z, height);
				for (int y = 0; y < height; y++) grid[x + y * width] = d[y];
			}

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++) f[x] = grid[x + y * width];
				DistanceTransform1D(f, d, v, z, width);
				for (int x = 0; x < width; x++) grid[x + y * width] = d[x];
			}

			float[] result = new float[grid.Length];
			for (int i = 0; i < grid.Length; i++)
			{
				result[i] = Mathf.Max(pixels[i].a, Mathf.Clamp01(radius + 0.5f - Mathf.Sqrt(grid[i])));
			}

			return result;
		}

		// Felzenszwalb & Huttenlocher squared distance transform of a sampled function.
		private static void DistanceTransform1D(float[] f, float[] d, int[] v, float[] z, int n)
		{
			int k = 0;
			v[0] = 0;
			z[0] = float.NegativeInfinity;
			z[1] = float.PositiveInfinity;
			for (int q = 1; q < n; q++)
			{
				float s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2f * q - 2f * v[k]);
				while (s <= z[k])
				{
					k--;
					s = ((f[q] + q * q) - (f[v[k]] + v[k] * v[k])) / (2f * q - 2f * v[k]);
				}

				k++;
				v[k] = q;
				z[k] = s;
				z[k + 1] = float.PositiveInfinity;
			}

			k = 0;
			for (int q = 0; q < n; q++)
			{
				while (z[k + 1] < q) k++;
				float dq = q - v[k];
				d[q] = dq * dq + f[v[k]];
			}
		}

		/// <summary>Area-averaging downscale of a square image (good quality for icon sizes).</summary>
		public static Color[] Downscale(Color[] source, int sourceSize, int targetSize)
		{
			if (targetSize == sourceSize)
			{
				return (Color[])source.Clone();
			}

			Color[] result = new Color[targetSize * targetSize];
			float scale = (float)sourceSize / targetSize;
			for (int ty = 0; ty < targetSize; ty++)
			{
				float y0 = ty * scale, y1 = y0 + scale;
				for (int tx = 0; tx < targetSize; tx++)
				{
					float x0 = tx * scale, x1 = x0 + scale;
					Color sum = Color.clear;
					float weight = 0f;
					for (int sy = (int)y0; sy < Mathf.Min(sourceSize, Mathf.CeilToInt(y1)); sy++)
					{
						float wy = Mathf.Min(y1, sy + 1) - Mathf.Max(y0, sy);
						for (int sx = (int)x0; sx < Mathf.Min(sourceSize, Mathf.CeilToInt(x1)); sx++)
						{
							float w = wy * (Mathf.Min(x1, sx + 1) - Mathf.Max(x0, sx));
							sum += source[sx + sy * sourceSize] * w;
							weight += w;
						}
					}

					result[tx + ty * targetSize] = sum / weight;
				}
			}

			return result;
		}

		public static void WritePng(Color[] pixels, int width, int height, string assetPath, bool keepAlpha)
		{
			Texture2D texture = new Texture2D(width, height, keepAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false, false);
			texture.SetPixels(pixels);
			texture.Apply();
			byte[] png = texture.EncodeToPNG();
			Object.DestroyImmediate(texture);

			string fullPath = Path.GetFullPath(assetPath);
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			File.WriteAllBytes(fullPath, png);
		}

		#endregion
	}
}
