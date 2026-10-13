using BunnyHeaven.UI;
using MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lawnmower.MainMenuEditor
{
	/// <summary>
	/// Builds the Main Menu screen prefab (Figma 12:5) from the Bunny Heaven UI kit, and sets up the MainMenu scene
	/// around it. Positions are Figma frame points (390x844): the title, ribbon and buttons keep their distance from the
	/// safe area's top or bottom edge (Figma assumes a 47pt status bar and a 34pt home indicator), and the lawn card
	/// stays centred between them, so taller screens only add space around the card.
	/// </summary>
	public static class MainMenuBuilder
	{
		public const string PrefabPath = "Assets/Prefabs/UI/MainMenu/MainMenuScreen.prefab";
		public const string ScenePath = "Assets/Scenes/MainMenu.unity";

		private const string KitDir = "Assets/Prefabs/UI/BunnyHeaven";
		private const string SpriteDir = "Assets/UI/BunnyHeaven/Sprites";
		private const string ThemePath = "Assets/UI/BunnyHeaven/Theme/BunnyHeavenTheme.asset";
		private const string PreviewPath = "Assets/Art/Previews/Level01_Preview.png";
		private const string LevelDataManagerPath = "Assets/ScriptableObjects/Game/Levels/Functionals/LevelDataManager.asset";
		private const string CurrentMowerManagerPath = "Assets/ScriptableObjects/Game/Mower/Functionals/CurrentMowerManager.asset";

		private const float FrameWidth = 390, FrameHeight = 844;
		private const float SafeTop = 47, SafeBottom = 810;
		private static readonly Vector4 Overhang = new(8, 8, 8, 12);

		private static UiTheme _theme;

		private enum Edge
		{
			Top,
			Middle,
			Bottom
		}

		[MenuItem("Bunny Heaven/Main Menu/Rebuild Screen Prefab")]
		public static void BuildPrefab()
		{
			_theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));

			GameObject root = BuildScreen();
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);
			AssetDatabase.SaveAssets();
		}

		/// <summary>Replaces everything in the MainMenu scene with the screen prefab, a UI camera, the EventSystem and the composition root.</summary>
		[MenuItem("Bunny Heaven/Main Menu/Rebuild Scene")]
		public static void BuildScene()
		{
			if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
			{
				BuildPrefab();
			}

			Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

			foreach (GameObject go in scene.GetRootGameObjects())
			{
				Object.DestroyImmediate(go);
			}

			var cameraGo = new GameObject("UiCamera");
			var camera = cameraGo.AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = UiTheme.Hex(0xFFC94A);
			camera.cullingMask = 0;
			camera.orthographic = true;
			cameraGo.tag = "MainCamera";

			var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

			var screen = (GameObject) PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
			screen.name = "MainMenuScreen";

			var rootGo = new GameObject("MainMenu");
			var manager = rootGo.AddComponent<MainMenuSceneManager>();
			SetRefs(manager,
				("view", screen.GetComponent<MainMenuView>()),
				("levelDataManager", AssetDatabase.LoadAssetAtPath<ScriptableObject>(LevelDataManagerPath)),
				("currentMowerManager", AssetDatabase.LoadAssetAtPath<ScriptableObject>(CurrentMowerManagerPath)));

			RenderSettings.skybox = null;
			RenderSettings.ambientMode = AmbientMode.Flat;

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
		}

		[MenuItem("Bunny Heaven/Main Menu/Rebuild Prefab And Scene")]
		public static void BuildAll()
		{
			BuildPrefab();
			BuildScene();
		}

		// ---------- Screen ----------

		private static GameObject BuildScreen()
		{
			var root = new GameObject("MainMenuScreen", typeof(RectTransform)) { layer = 5 };
			var canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			var scaler = root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(FrameWidth, FrameHeight);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
			root.AddComponent<GraphicRaycaster>();
			var rootRect = (RectTransform) root.transform;

			RectTransform background = Kit("ScreenBackground", rootRect);
			Stretch(background, 0, 0, 0, 0);

			RectTransform safe = NewUI("SafeArea", rootRect);
			Stretch(safe, 0, 0, 0, 0);
			safe.gameObject.AddComponent<Lawnmower.Screens.SafeAreaPanel>().SetMinInsets(0f, 0f);

			LongPressTrigger title = BuildTitle(safe);
			BuildRibbon(safe);
			BuildLawnCard(safe);

			var play = Kit("BigButton_Green", safe).GetComponent<ButtonView>();
			play.name = "PlayButton";
			play.SetIcon(Icon("play"));
			play.SetLabel("PLAY · LEVEL 1");
			Place((RectTransform) play.transform, 45, 600, 300, 70, Edge.Bottom);

			ButtonView levels = RoundButton(safe, "LevelsButton", ButtonColor.Blue, "levels", "LEVELS", 89);
			ButtonView settings = RoundButton(safe, "SettingsButton", ButtonColor.Red, "settings", "SETTINGS", 187);
			ButtonView howToPlay = RoundButton(safe, "HowToPlayButton", ButtonColor.Green, "howtoplay", "HOW TO PLAY", 293);

			// Floats up over the bottom of the lawn card, clear of the buttons.
			var toast = Kit("Toast", safe).GetComponent<ToastView>();
			var toastRect = (RectTransform) toast.transform;
			toastRect.anchorMin = new Vector2(0, 0.5f);
			toastRect.anchorMax = new Vector2(1, 0.5f);
			toastRect.pivot = new Vector2(0.5f, 0.5f);
			toastRect.sizeDelta = new Vector2(0, 60);
			toastRect.anchoredPosition = new Vector2(0, (SafeTop + SafeBottom) / 2 - 530);

			var view = root.AddComponent<MainMenuView>();
			SetRefs(view,
				("playButton", play),
				("levelsButton", levels),
				("settingsButton", settings),
				("howToPlayButton", howToPlay),
				("title", title),
				("toast", toast));
			return root;
		}

		private static LongPressTrigger BuildTitle(RectTransform parent)
		{
			RectTransform title = NewUI("Title", parent);
			Place(title, 30, 58, 330, 197, Edge.Top);
			var trigger = title.gameObject.AddComponent<LongPressTrigger>();

			// Figma: "LAWN" 105,58,181,109 and "MOWER" 75,146,240,109; the lines overlap by ~21pt.
			TitleLine(title, "Line1", "BUNNY", 0);
			TitleLine(title, "Line2", "HEAVEN", 88);
			return trigger;
		}

		private static void TitleLine(RectTransform parent, string name, string text, float top)
		{
			TextMeshProUGUI tmp = Text(parent, name, text, 92, 4, ThemeColor.White, inkShadow: true);
			tmp.raycastTarget = true;
			RectTransform rect = tmp.rectTransform;
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = new Vector2(1, 1);
			rect.pivot = new Vector2(0.5f, 1);
			rect.sizeDelta = new Vector2(0, 109);
			rect.anchoredPosition = new Vector2(0, -top);
		}

		private static void BuildRibbon(RectTransform parent)
		{
			RectTransform ribbon = NewUI("Ribbon", parent);
			Place(ribbon, 103, 258, 183, 45, Edge.Top);
			ribbon.localRotation = Quaternion.Euler(0, 0, -3);
			Frame(ribbon, "Ribbon", ThemeColor.Red, ThemeColor.RedShadow);

			TextMeshProUGUI label = Text(ribbon, "Label", "A MOWING PUZZLE", 20, 6, ThemeColor.White);
			Stretch(label.rectTransform, 0, 0, 0, 2);
		}

		private static void BuildLawnCard(RectTransform parent)
		{
			RectTransform card = Kit("Card", parent);
			card.name = "LawnCard";
			Place(card, 70, 303, 259, 271, Edge.Middle);
			card.localRotation = Quaternion.Euler(0, 0, 2);

			RectTransform lawn = NewUI("Lawn", card);
			Stretch(lawn, 14, 18, 14, 14);
			var image = lawn.gameObject.AddComponent<Image>();
			image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PreviewPath);
			image.preserveAspect = true;
			image.raycastTarget = false;
		}

		private static ButtonView RoundButton(RectTransform parent, string name, ButtonColor color, string icon, string label,
			float centreX)
		{
			var button = Kit("RoundIconButton", parent).GetComponent<ButtonView>();
			button.name = name;
			button.SetColor(color);
			button.SetIcon(Icon(icon));
			button.SetLabel(label);

			var rect = (RectTransform) button.transform;
			rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
			rect.pivot = new Vector2(0.5f, 1);
			rect.anchoredPosition = new Vector2(centreX - FrameWidth / 2, SafeBottom - 696);
			return button;
		}

		// ---------- Helpers ----------

		/// <summary>Positions a rect given its Figma frame box, keeping its distance from the given safe-area edge.</summary>
		private static void Place(RectTransform rect, float x, float y, float width, float height, Edge edge)
		{
			float anchorY = edge switch { Edge.Top => 1f, Edge.Bottom => 0f, _ => 0.5f };
			float edgeY = edge switch { Edge.Top => SafeTop, Edge.Bottom => SafeBottom, _ => (SafeTop + SafeBottom) / 2 };

			rect.anchorMin = rect.anchorMax = new Vector2(0.5f, anchorY);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(width, height);
			rect.anchoredPosition = new Vector2(x + width / 2 - FrameWidth / 2, edgeY - (y + height / 2));
		}

		private static RectTransform Kit(string prefab, RectTransform parent)
		{
			var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitDir}/{prefab}.prefab");
			var go = (GameObject) PrefabUtility.InstantiatePrefab(asset, parent);
			return (RectTransform) go.transform;
		}

		private static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/Icons/Icon_{name}.png");

		private static RectTransform NewUI(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
			var rect = (RectTransform) go.transform;
			rect.SetParent(parent, false);
			return rect;
		}

		private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
		{
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(left, bottom);
			rect.offsetMax = new Vector2(-right, -top);
		}

		/// <summary>A kit-style hand-drawn frame (tinted body over an offset shadow), as the kit builds them.</summary>
		private static void Frame(RectTransform parent, string frame, ThemeColor fill, ThemeColor shadow)
		{
			Image shadowImage = FrameLayer(parent, "FrameShadow", $"Frames/Frame_{frame}_Shadow", shadow);
			Image fillImage = FrameLayer(parent, "FrameFill", $"Frames/Frame_{frame}", fill);
			shadowImage.transform.SetAsFirstSibling();
			fillImage.transform.SetSiblingIndex(1);

			var view = parent.gameObject.AddComponent<FrameView>();
			SetRefs(view, ("fill", fillImage.GetComponent<ThemedGraphic>()), ("shadow", shadowImage.GetComponent<ThemedGraphic>()));
		}

		private static Image FrameLayer(RectTransform parent, string name, string sprite, ThemeColor token)
		{
			RectTransform rect = NewUI(name, parent);
			Stretch(rect, -Overhang.x, -Overhang.y, -Overhang.z, -Overhang.w);
			var image = rect.gameObject.AddComponent<Image>();
			image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{sprite}.png");
			image.type = Image.Type.Sliced;
			image.pixelsPerUnitMultiplier = 1f;
			image.raycastTarget = false;
			Themed(rect.gameObject, token);
			return image;
		}

		private static TextMeshProUGUI Text(RectTransform parent, string name, string text, float size, float spacingPercent,
			ThemeColor color, bool inkShadow = false)
		{
			RectTransform rect = NewUI(name, parent);
			var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.fontSize = size;
			tmp.characterSpacing = spacingPercent;
			tmp.alignment = TextAlignmentOptions.Center;
			tmp.textWrappingMode = TextWrappingModes.NoWrap;
			tmp.overflowMode = TextOverflowModes.Overflow;
			tmp.raycastTarget = false;
			var themedText = rect.gameObject.AddComponent<ThemedText>();
			themedText.Theme = _theme;
			themedText.Font = ThemeFont.Heading;
			themedText.InkShadow = inkShadow;
			Themed(rect.gameObject, color);
			return tmp;
		}

		private static void Themed(GameObject go, ThemeColor token)
		{
			var themed = go.AddComponent<ThemedGraphic>();
			themed.Theme = _theme;
			themed.Token = token;
		}

		private static void SetRefs(Object target, params (string field, Object value)[] refs)
		{
			var so = new SerializedObject(target);
			foreach ((string field, Object value) in refs)
			{
				so.FindProperty(field).objectReferenceValue = value;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
		}
	}
}
