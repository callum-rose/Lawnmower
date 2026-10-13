using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Lawnmower.Editor.BunnyHeaven
{
	/// <summary>
	/// Renders the RegularBunny mower onto the Bunny Heaven yellow with an ink outline, writes every
	/// iOS icon size to <see cref="OutputDir"/> and assigns them in Player Settings. Also sets the splash.
	/// </summary>
	public static class AppIconRenderer
	{
		public const string BunnyPrefabPath = "Assets/Prefabs/Game/Mowers/Complete/RegularBunny.prefab";
		public const string OutputDir = "Assets/Art/Icon";
		public const int MasterSize = 1024;
		public const string LaunchImagePath = OutputDir + "/LaunchScreen_Yellow.png";

		// Figma tokens (same values as the UI kit's UiTheme: Yellow, YellowShadow, Ink).
		public static readonly Color Yellow = Hex(0xFFC94A);
		public static readonly Color YellowShadow = Hex(0xE9A92C);
		public static readonly Color Ink = Hex(0x3B2A20);

		// Proportions of the 1024 master.
		private const float BunnyFill = 0.76f;
		private const float OutlineWidth = 18f;
		private const float ShadowOffset = 16f;

		private static readonly Vector3 ViewEuler = new Vector3(18f, 205f, 0f);

		[MenuItem("Bunny Heaven/Render App Icon")]
		public static void RenderAppIconMenu()
		{
			string master = RenderAppIcon();
			ApplySplash();
			EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(master));
		}

		/// <summary>Renders, imports and assigns every icon. Returns the 1024 master's asset path.</summary>
		public static string RenderAppIcon()
		{
			Color[] master = RenderMaster();

			Dictionary<int, string> written = new Dictionary<int, string>();
			string WriteSize(int size)
			{
				if (written.TryGetValue(size, out string existing))
				{
					return existing;
				}

				string path = $"{OutputDir}/AppIcon_{size}.png";
				OffscreenRenderer.WritePng(OffscreenRenderer.Downscale(master, MasterSize, size), size, size, path, keepAlpha: false);
				written[size] = path;
				return path;
			}

			string masterPath = WriteSize(MasterSize);

			PlatformIcon[] icons = PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS)
				.SelectMany(kind => PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind))
				.ToArray();
			foreach (PlatformIcon icon in icons)
			{
				WriteSize(icon.width);
			}

			AssetDatabase.Refresh();
			foreach (string path in written.Values)
			{
				ConfigureImporter(path);
			}

			foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
			{
				PlatformIcon[] kindIcons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
				foreach (PlatformIcon icon in kindIcons)
				{
					icon.SetTexture(AssetDatabase.LoadAssetAtPath<Texture2D>(written[icon.width]));
				}

				PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, kindIcons);
			}

			// The default icon is what the Editor and other platforms fall back to.
			PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(masterPath) }, IconKind.Any);

			AssetDatabase.SaveAssets();
			Debug.Log($"[AppIconRenderer] Wrote {written.Count} icon sizes ({string.Join(", ", written.Keys.OrderBy(k => k))}) and assigned {icons.Length} iOS icon slots.");
			return masterPath;
		}

		/// <summary>
		/// Yellow splash and launch screen. A Personal licence forces the Unity splash, so it stays on with
		/// the Unity logo alone (dark on light, to read on yellow); with Pro the splash is turned off.
		/// </summary>
		[MenuItem("Bunny Heaven/Apply Splash Screen")]
		public static void ApplySplash()
		{
			PlayerSettings.SplashScreen.backgroundColor = Yellow;
			PlayerSettings.SplashScreen.background = null;
			PlayerSettings.SplashScreen.backgroundPortrait = null;
			PlayerSettings.SplashScreen.overlayOpacity = 0f;
			PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[0];
			PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
			PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow;
			PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;

			bool forced = !Application.HasProLicense();
			PlayerSettings.SplashScreen.show = forced;
			PlayerSettings.SplashScreen.showUnityLogo = forced;

			// iOS shows its own launch screen before Unity starts: a yellow background plus a small
			// solid-yellow image (Unity only honours the background colour when an image is set).
			Texture2D launch = WriteSolidTexture(LaunchImagePath, Yellow);
			PlayerSettings.iOS.SetiPhoneLaunchScreenType(iOSLaunchScreenType.ImageAndBackgroundRelative);
			PlayerSettings.iOS.SetiPadLaunchScreenType(iOSLaunchScreenType.ImageAndBackgroundRelative);
			PlayerSettings.iOS.SetLaunchScreenImage(launch, iOSLaunchScreenImageType.iPhonePortraitImage);
			PlayerSettings.iOS.SetLaunchScreenImage(launch, iOSLaunchScreenImageType.iPhoneLandscapeImage);
			PlayerSettings.iOS.SetLaunchScreenImage(launch, iOSLaunchScreenImageType.iPadImage);

			SerializedObject playerSettings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
			foreach (string property in new[] { "iOSLaunchScreenBackgroundColor", "iOSLaunchScreeniPadBackgroundColor" })
			{
				SerializedProperty background = playerSettings.FindProperty(property);
				if (background != null)
				{
					background.colorValue = Yellow;
				}
			}

			playerSettings.ApplyModifiedPropertiesWithoutUndo();

			AssetDatabase.SaveAssets();
			Debug.Log($"[AppIconRenderer] Splash: yellow, Unity splash {(forced ? "kept (licence requires it), Unity logo only" : "off")}.");
		}

		private static Color[] RenderMaster()
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BunnyPrefabPath);
			if (prefab == null)
			{
				throw new FileNotFoundException("RegularBunny prefab not found", BunnyPrefabPath);
			}

			using OffscreenRenderer renderer = new OffscreenRenderer();
			GameObject bunny = renderer.Instantiate(prefab);
			bunny.SetActive(true);
			bunny.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
			PoseIdle(bunny);

			Bounds bounds = RendererBounds(bunny);
			Camera camera = renderer.Camera;
			camera.orthographic = true;
			camera.transform.rotation = Quaternion.Euler(ViewEuler);
			camera.transform.position = bounds.center - camera.transform.forward * (bounds.extents.magnitude * 4f);
			camera.orthographicSize = bounds.extents.magnitude;
			camera.nearClipPlane = 0.01f;
			camera.farClipPlane = bounds.extents.magnitude * 10f;

			// Two passes: render loosely, then re-frame so the silhouette fills BunnyFill of the canvas, centred.
			Color[] pixels = renderer.RenderStraightAlpha(MasterSize, MasterSize);
			for (int pass = 0; pass < 2; pass++)
			{
				RectInt opaque = OffscreenRenderer.OpaqueBounds(pixels, MasterSize, MasterSize);
				if (opaque.width == 0)
				{
					throw new System.InvalidOperationException("RegularBunny rendered nothing; check its renderers and the URP asset.");
				}

				float worldPerPixel = camera.orthographicSize * 2f / MasterSize;
				Vector2 offsetPixels = opaque.center - new Vector2(MasterSize, MasterSize) * 0.5f;
				camera.transform.position += (camera.transform.right * offsetPixels.x + camera.transform.up * offsetPixels.y) * worldPerPixel;
				camera.orthographicSize *= Mathf.Max(opaque.width, opaque.height) / (MasterSize * BunnyFill);
				pixels = renderer.RenderStraightAlpha(MasterSize, MasterSize);
			}

			return Composite(pixels, MasterSize);
		}

		private static Color[] Composite(Color[] bunny, int size)
		{
			float[] outline = OffscreenRenderer.Dilate(bunny, size, size, OutlineWidth);
			int shift = Mathf.RoundToInt(ShadowOffset);

			Color[] result = new Color[bunny.Length];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					int i = x + y * size;

					// Offset cut-out shadow (down-right), like the Figma cards.
					int sx = x - shift, sy = y + shift;
					float shadow = sx >= 0 && sy < size ? outline[sx + sy * size] : 0f;

					Color c = Color.Lerp(Yellow, YellowShadow, shadow);
					c = Color.Lerp(c, Ink, outline[i]);
					c = Color.Lerp(c, bunny[i], bunny[i].a);
					c.a = 1f;
					result[i] = c;
				}
			}

			return result;
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

		private static Bounds RendererBounds(GameObject root)
		{
			Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
				.Where(r => r.enabled && !(r is ParticleSystemRenderer))
				.ToArray();
			if (renderers.Length == 0)
			{
				throw new System.InvalidOperationException($"{root.name} has no renderers");
			}

			Bounds bounds = renderers[0].bounds;
			foreach (Renderer r in renderers.Skip(1))
			{
				bounds.Encapsulate(r.bounds);
			}

			return bounds;
		}

		private static Texture2D WriteSolidTexture(string path, Color color)
		{
			const int size = 16;
			OffscreenRenderer.WritePng(Enumerable.Repeat(color, size * size).ToArray(), size, size, path, keepAlpha: false);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
			ConfigureImporter(path);
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		private static void ConfigureImporter(string path)
		{
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
			importer.textureType = TextureImporterType.Default;
			importer.npotScale = TextureImporterNPOTScale.None;
			importer.mipmapEnabled = false;
			importer.alphaSource = TextureImporterAlphaSource.None;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.maxTextureSize = MasterSize;
			importer.isReadable = false;
			importer.SaveAndReimport();
		}

		private static Color Hex(int rgb)
		{
			return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
		}
	}
}
