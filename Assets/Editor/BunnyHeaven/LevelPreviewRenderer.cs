using System.IO;
using System.Linq;
using Game.Levels;
using Game.Tiles;
using Lawnmower.Model;
using UnityEditor;
using UnityEngine;

namespace Lawnmower.Editor.BunnyHeaven
{
	/// <summary>
	/// Renders a level's board (plus the RegularBunny on its start tile) isometrically, on a transparent
	/// background, to a PNG imported as a Sprite. Used for the Main Menu board preview.
	/// </summary>
	public static class LevelPreviewRenderer
	{
		public const string OutputDir = "Assets/Art/Previews";
		public const int DefaultSize = 1024;

		private static readonly Vector3 ViewEuler = new Vector3(35f, 45f, 0f);
		private const float Padding = 0.04f;

		[MenuItem("Bunny Heaven/Render Level Preview")]
		public static void RenderSelectedLevelMenu()
		{
			LevelData level = Selection.activeObject as LevelData;
			if (level == null)
			{
				EditorUtility.DisplayDialog("Render Level Preview", "Select a Level Data asset in the Project window first.", "OK");
				return;
			}

			string path = RenderLevelPreview(level, $"{OutputDir}/{level.name}_Preview.png");
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Sprite>(path));
		}

		[MenuItem("Bunny Heaven/Render Level Preview", true)]
		private static bool ValidateRenderSelectedLevelMenu() => Selection.activeObject is LevelData;

		/// <summary>Asset-path overload for scripts and CLI eval.</summary>
		public static string RenderLevelPreview(string levelAssetPath, string outputAssetPath, int size = DefaultSize, bool includeBunny = true)
		{
			LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(levelAssetPath);
			if (level == null)
			{
				throw new FileNotFoundException("No LevelData at path", levelAssetPath);
			}

			return RenderLevelPreview(level, outputAssetPath, size, includeBunny);
		}

		/// <summary>
		/// Renders <paramref name="level"/> to <paramref name="outputAssetPath"/> (a .png under Assets/),
		/// imports it as a Sprite and returns the path. The level asset is read only, never modified.
		/// </summary>
		internal static string RenderLevelPreview(LevelData level, string outputAssetPath, int size = DefaultSize, bool includeBunny = true)
		{
			TilePrefabsManager tilePrefabs = LoadFirst<TilePrefabsManager>();

			// Fresh runtime tiles: the asset itself is never mutated.
			Level copy = level.CreateLevel();

			using OffscreenRenderer renderer = new OffscreenRenderer();
			GameObject board = new GameObject("Board");
			renderer.Add(board);

			for (int y = 0; y < copy.Depth; y++)
			{
				for (int x = 0; x < copy.Width; x++)
				{
					Tile tile = copy.GetTile(x, y);
					BaseTileObject tileObject = tilePrefabs.GetPrefabAndInstantiate(tile);
					if (tileObject == null)
					{
						continue;
					}

					tileObject.Bind(tile);
					tileObject.name = $"Tile ({x}, {y})";
					tileObject.transform.SetParent(board.transform, false);
					tileObject.transform.localPosition = TileCentre(x, y);
				}
			}

			if (includeBunny)
			{
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AppIconRenderer.BunnyPrefabPath);
				GameObject bunny = renderer.Instantiate(prefab);
				bunny.SetActive(true);
				bunny.transform.SetParent(board.transform, false);
				bunny.transform.localPosition = TileCentre(copy.StartPosition.x, copy.StartPosition.y);
				bunny.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
			}

			Camera camera = renderer.Camera;
			camera.orthographic = true;
			camera.transform.rotation = Quaternion.Euler(ViewEuler);
			FitOrthographic(camera, BoardBounds(board), Padding);

			Color[] pixels = renderer.RenderStraightAlpha(size, size);

			// Trim the empty band above/below a wide board so the sprite's rect is the board itself.
			RectInt crop = OffscreenRenderer.OpaqueBounds(pixels, size, size);
			if (crop.width == 0)
			{
				throw new System.InvalidOperationException($"{level.name} rendered nothing; check the tile prefabs and the URP asset.");
			}

			int pad = Mathf.RoundToInt(size * Padding * 0.5f);
			crop = new RectInt(0, Mathf.Max(0, crop.yMin - pad), size, 0) { yMax = Mathf.Min(size, crop.yMax + pad) };
			Color[] cropped = new Color[crop.width * crop.height];
			for (int y = 0; y < crop.height; y++)
			{
				System.Array.Copy(pixels, (crop.yMin + y) * size, cropped, y * crop.width, crop.width);
			}

			OffscreenRenderer.WritePng(cropped, crop.width, crop.height, outputAssetPath, keepAlpha: true);
			AssetDatabase.ImportAsset(outputAssetPath, ImportAssetOptions.ForceUpdate);

			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(outputAssetPath);
			importer.textureType = TextureImporterType.Sprite;
			importer.spriteImportMode = SpriteImportMode.Single;
			importer.alphaIsTransparency = true;
			importer.mipmapEnabled = false;
			importer.npotScale = TextureImporterNPOTScale.None;
			importer.maxTextureSize = Mathf.Max(size, 32);
			importer.SaveAndReimport();

			Debug.Log($"[LevelPreviewRenderer] Rendered {level.name} ({copy.Width}x{copy.Depth}) to {outputAssetPath}");
			return outputAssetPath;
		}

		private static Vector3 TileCentre(int x, int y)
		{
			return new Vector3((x + 0.5f) * LevelDimensions.TileSize, LevelDimensions.LevelBasePlaneY, (y + 0.5f) * LevelDimensions.TileSize);
		}

		private static Bounds BoardBounds(GameObject board)
		{
			Renderer[] renderers = board.GetComponentsInChildren<Renderer>()
				.Where(r => r.enabled && !(r is ParticleSystemRenderer))
				.ToArray();
			Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(board.transform.position, Vector3.one);
			foreach (Renderer r in renderers)
			{
				bounds.Encapsulate(r.bounds);
			}

			return bounds;
		}

		/// <summary>Centres an orthographic camera on the bounds' projected box, with fractional padding.</summary>
		private static void FitOrthographic(Camera camera, Bounds bounds, float padding)
		{
			Transform t = camera.transform;
			Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
			Vector2 max = new Vector2(float.MinValue, float.MinValue);
			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
				Vector3 local = Quaternion.Inverse(t.rotation) * (corner - bounds.center);
				min = Vector2.Min(min, local);
				max = Vector2.Max(max, local);
			}

			Vector2 centre = (min + max) * 0.5f;
			float distance = bounds.extents.magnitude * 4f;
			t.position = bounds.center + t.right * centre.x + t.up * centre.y - t.forward * distance;
			camera.orthographicSize = Mathf.Max(max.x - min.x, max.y - min.y) * 0.5f * (1f + padding * 2f);
			camera.nearClipPlane = 0.01f;
			camera.farClipPlane = distance * 2f;
		}

		private static T LoadFirst<T>() where T : Object
		{
			string guid = AssetDatabase.FindAssets($"t:{typeof(T).Name}").FirstOrDefault();
			T asset = guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
			if (asset == null)
			{
				throw new FileNotFoundException($"No {typeof(T).Name} asset found");
			}

			return asset;
		}
	}
}
