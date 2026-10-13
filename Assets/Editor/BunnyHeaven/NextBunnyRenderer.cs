using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lawnmower.Editor.BunnyHeaven
{
	/// <summary>
	/// Renders the EggBunny (Idle pose, 3/4 view) as a transparent sprite with an ink outline: the bunny standing
	/// on Level Select's "NEXT!" node.
	/// </summary>
	public static class NextBunnyRenderer
	{
		public const string OutputPath = "Assets/Art/LevelSelect/NextBunny.png";

		private const string BunnyPrefabPath = "Assets/Prefabs/Game/Mowers/Complete/EggBunny.prefab";
		private const int Size = 256;
		private const float Fill = 0.86f;
		private const float OutlineWidth = 5f;

		private static readonly Vector3 ViewEuler = new Vector3(18f, 205f, 0f);
		private static readonly Color Ink = new Color(0x3B / 255f, 0x2A / 255f, 0x20 / 255f);

		[MenuItem("Bunny Heaven/Render Level Select Bunny")]
		public static void RenderMenu() => EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Sprite>(Render()));

		public static string Render()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BunnyPrefabPath);
			if (prefab == null)
			{
				throw new FileNotFoundException("EggBunny prefab not found", BunnyPrefabPath);
			}

			Color[] pixels;
			using (OffscreenRenderer renderer = new OffscreenRenderer())
			{
				GameObject bunny = renderer.Instantiate(prefab);
				bunny.SetActive(true);
				bunny.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
				PoseIdle(bunny);

				Renderer[] renderers = bunny.GetComponentsInChildren<Renderer>(true)
					.Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
				Bounds bounds = renderers[0].bounds;
				foreach (Renderer r in renderers.Skip(1))
				{
					bounds.Encapsulate(r.bounds);
				}

				Camera camera = renderer.Camera;
				camera.orthographic = true;
				camera.transform.rotation = Quaternion.Euler(ViewEuler);
				camera.transform.position = bounds.center - camera.transform.forward * (bounds.extents.magnitude * 4f);
				camera.orthographicSize = bounds.extents.magnitude;
				camera.nearClipPlane = 0.01f;
				camera.farClipPlane = bounds.extents.magnitude * 10f;

				pixels = renderer.RenderStraightAlpha(Size, Size);
				for (int pass = 0; pass < 2; pass++)
				{
					RectInt opaque = OffscreenRenderer.OpaqueBounds(pixels, Size, Size);
					if (opaque.width == 0)
					{
						throw new System.InvalidOperationException("EggBunny rendered nothing");
					}

					float worldPerPixel = camera.orthographicSize * 2f / Size;
					// Centre horizontally; stand the feet near the bottom edge.
					Vector2 offset = new Vector2(opaque.center.x - Size * 0.5f, opaque.yMin - Size * (1f - Fill) * 0.5f);
					camera.transform.position += (camera.transform.right * offset.x + camera.transform.up * offset.y) * worldPerPixel;
					camera.orthographicSize *= Mathf.Max(opaque.width, opaque.height) / (Size * Fill);
					pixels = renderer.RenderStraightAlpha(Size, Size);
				}
			}

			float[] outline = OffscreenRenderer.Dilate(pixels, Size, Size, OutlineWidth);
			Color[] result = new Color[pixels.Length];
			for (int i = 0; i < pixels.Length; i++)
			{
				Color bunny = pixels[i];
				Color c = Color.Lerp(Ink, bunny, bunny.a);
				c.a = Mathf.Max(outline[i], bunny.a);
				result[i] = c;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
			OffscreenRenderer.WritePng(result, Size, Size, OutputPath, keepAlpha: true);
			AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);

			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(OutputPath);
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.spritePivot = new Vector2(0.5f, 0f);
			importer.SaveAndReimport();

			Debug.Log($"[NextBunnyRenderer] Wrote {OutputPath}");
			return OutputPath;
		}

		private static void PoseIdle(GameObject bunny)
		{
			Animator animator = bunny.GetComponentInChildren<Animator>(true);
			if (animator == null || animator.runtimeAnimatorController == null)
			{
				return;
			}

			AnimationClip idle = animator.runtimeAnimatorController.animationClips
				.FirstOrDefault(c => c.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0);
			if (idle != null)
			{
				idle.SampleAnimation(animator.gameObject, 0f);
			}
		}
	}
}
