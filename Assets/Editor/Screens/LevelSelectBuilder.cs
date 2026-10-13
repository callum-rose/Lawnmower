using System;
using BunnyHeaven.UI;
using BunnyHeaven.UI.Editor;
using LevelSelect;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Lawnmower.Screens.Editor.ScreenBuilderUtil;
using Object = UnityEngine.Object;

namespace Lawnmower.Screens.Editor
{
	/// <summary>
	/// Builds the Level Select prefabs (Figma 12:204) from the kit and lays out the LevelSelect scene. Re-run after a
	/// layout change; once someone hand-tweaks the prefabs in the editor, edit them there instead.
	/// </summary>
	public static class LevelSelectBuilder
	{
		public const string Dir = "Assets/Prefabs/UI/LevelSelect";
		public const string NodePath = Dir + "/LevelNode.prefab";
		public const string ScreenPath = Dir + "/LevelSelectScreen.prefab";
		public const string ScenePath = "Assets/Scenes/LevelSelect.unity";
		public const string BunnySpritePath = "Assets/Art/LevelSelect/NextBunny.png";

		private const string LevelDataManagerPath = "Assets/ScriptableObjects/Game/Levels/Functionals/LevelDataManager.asset";
		private const string CurrentMowerManagerPath = "Assets/ScriptableObjects/Game/Mower/Functionals/CurrentMowerManager.asset";

		/// <summary>Figma node centres (frame px), level 1 first.</summary>
		private static readonly Vector2[] FigmaCentres =
		{
			new Vector2(110, 772), new Vector2(205, 724), new Vector2(292, 662), new Vector2(248, 588), new Vector2(146, 540),
			new Vector2(90, 462), new Vector2(176, 398), new Vector2(284, 350), new Vector2(246, 268), new Vector2(136, 226)
		};

		private const float PathBottomFigmaY = 804f;
		private const float BottomPadding = 26f;

		// Figma's #F3B73A (tab track, locked node face) isn't a theme token.
		private static readonly Color TabTrack = new Color(0xF3 / 255f, 0xB7 / 255f, 0x3A / 255f);

		[MenuItem("Bunny Heaven/Screens/Build Level Select")]
		public static void BuildAll()
		{
			BuildNode();
			BuildScreen();
			AssetDatabase.SaveAssets();
		}

		[MenuItem("Bunny Heaven/Screens/Rebuild LevelSelect Scene")]
		public static void BuildScene()
		{
			Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			Camera camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
			camera.tag = "MainCamera";
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Theme.Get(ThemeColor.Yellow);
			camera.cullingMask = 0;
			camera.orthographic = true;

			new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

			GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvasGo.layer = 5;
			Canvas canvas = canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			ConfigureScaler(canvasGo.GetComponent<CanvasScaler>());

			GameObject screenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenPath);
			GameObject screen = (GameObject)PrefabUtility.InstantiatePrefab(screenPrefab, canvasGo.transform);
			Stretch((RectTransform)screen.transform);

			GameObject root = new GameObject("SceneManager");
			Type managerType = typeof(LevelSelectScreenView).Assembly.GetType("LevelSelect.SceneManager", true);
			Component manager = root.AddComponent(managerType);
			SetRefs(manager,
				("screen", screen.GetComponent<LevelSelectScreenView>()),
				("currentMowerManager", AssetDatabase.LoadMainAssetAtPath(CurrentMowerManagerPath)),
				("levelDataManager", AssetDatabase.LoadMainAssetAtPath(LevelDataManagerPath)));

			EditorSceneManager.SaveScene(scene, ScenePath);
		}

		public static void ConfigureScaler(CanvasScaler scaler)
		{
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(390, 844);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
		}

		private static void BuildNode()
		{
			RectTransform root = NewUI("LevelNode", null, new Vector2(64, 64));

			RectTransform body = NewUI("Body", root, new Vector2(64, 64));
			body.anchorMin = body.anchorMax = body.pivot = new Vector2(0.5f, 0.5f);

			Image shadow = AddFrameLayer(body, "Shadow", "Circle_Shadow", ThemeColor.YellowShadow, Overhang, themed: false);
			Image face = AddFrameLayer(body, "Face", "Circle", ThemeColor.White, Overhang, themed: false);
			face.raycastTarget = true;

			TextMeshProUGUI number = AddText(body, "Number", "7", ThemeFont.Heading, 32, ThemeColor.White, 0f, themedColor: false);
			Stretch(number.rectTransform, 0, 0, 0, 3);
			number.alignment = TextAlignmentOptions.Center;

			Image lockIcon = AddIcon(body, "Lock", "lock", 24, ThemeColor.Brown);

			// Perfect: the yellow tick badge at the top-right (Figma: node x+44, y-4, 24x24).
			RectTransform badge = NewUI("PerfectBadge", body, new Vector2(24, 24));
			badge.anchorMin = badge.anchorMax = new Vector2(1, 1);
			badge.pivot = new Vector2(0.5f, 0.5f);
			badge.anchoredPosition = new Vector2(-8, -8);
			AddFrameLayer(badge, "Fill", "Chip", ThemeColor.Yellow, new Vector4(5, 5, 5, 5));
			AddIcon(badge, "Check", "check", 13, ThemeColor.Ink);

			// NEXT! ribbon over the node's top-left, tilted -6 degrees (Figma 68x36).
			RectTransform tag = NewUI("NextTag", body, new Vector2(68, 36));
			tag.anchorMin = tag.anchorMax = new Vector2(0, 1);
			tag.pivot = new Vector2(0.5f, 0.5f);
			tag.anchoredPosition = new Vector2(0, 10);
			tag.localEulerAngles = new Vector3(0, 0, 6);
			AddFrameLayer(tag, "Shadow", "Ribbon_Shadow", ThemeColor.RedShadow, new Vector4(6, 6, 6, 9));
			AddFrameLayer(tag, "Fill", "Ribbon", ThemeColor.Red, new Vector4(6, 6, 6, 9));
			TextMeshProUGUI tagText = AddText(tag, "Label", "NEXT!", ThemeFont.Heading, 18, ThemeColor.White, 4f);
			Stretch(tagText.rectTransform, 0, 0, 0, 3);

			// The bunny stands on the top of the node.
			Image bunny = AddImage(body, "Bunny", AssetDatabase.LoadAssetAtPath<Sprite>(BunnySpritePath), ThemeColor.White, themed: false);
			bunny.color = Color.white;
			bunny.preserveAspect = true;
			RectTransform bunnyRect = bunny.rectTransform;
			bunnyRect.anchorMin = bunnyRect.anchorMax = new Vector2(0.5f, 1f);
			bunnyRect.pivot = new Vector2(0.5f, 0f);
			bunnyRect.sizeDelta = new Vector2(84, 84);
			bunnyRect.anchoredPosition = new Vector2(12, -18);

			// Draw order: shadow, face, number, lock, badge, bunny, tag.
			tag.SetAsLastSibling();

			Button button = root.gameObject.AddComponent<Button>();
			button.transition = Selectable.Transition.None;
			button.targetGraphic = face;
			PressScale press = root.gameObject.AddComponent<PressScale>();
			SetRefs(press, ("target", body));

			LevelNodeView view = root.gameObject.AddComponent<LevelNodeView>();
			SetRefs(view,
				("theme", Theme),
				("body", body),
				("face", face),
				("shadow", shadow),
				("number", number),
				("lockIcon", lockIcon),
				("perfectBadge", badge.gameObject),
				("nextTag", tag.gameObject),
				("bunny", bunny.gameObject));
			view.Show(Lawnmower.Presenters.LevelNodeState.Next, 7);

			Save(root.gameObject, NodePath);
		}

		private static void BuildScreen()
		{
			RectTransform root = NewUI("LevelSelectScreen", null, new Vector2(390, 844));
			Stretch(root);

			RectTransform background = (RectTransform)Kit<Transform>("ScreenBackground", root);
			Stretch(background);

			RectTransform safe = NewUI("SafeArea", root);
			Stretch(safe);
			safe.gameObject.AddComponent<SafeAreaPanel>();

			// Header (Figma y minus the 48pt status strip the safe area provides).
			ButtonView back = AddBackButton(safe);

			TextMeshProUGUI title = AddText(safe, "Title", "LEVELS", ThemeFont.Heading, 42, ThemeColor.White, 4f, inkShadow: true);
			TopCentre(title.rectTransform, 4, 200, 50);

			PillView pill = Kit<PillView>("Pill", safe);
			pill.name = "PerfectsPill";
			pill.SetIcon(KitAssets.Icon("check"));
			pill.SetChipColor(ThemeColor.Green);
			pill.SetShadow(ThemeColor.YellowShadow);
			pill.SetText("0/10");
			pill.Label.fontSize = 18;
			pill.Label.characterSpacing = 2;
			RectTransform chip = (RectTransform)pill.transform.Find("Chip");
			Fixed(chip, 26, 26);
			((RectTransform)chip.Find("Icon")).sizeDelta = new Vector2(14, 14);
			HorizontalLayoutGroup pillLayout = pill.GetComponent<HorizontalLayoutGroup>();
			pillLayout.padding = new RectOffset(5, 14, 5, 5);
			pillLayout.spacing = 6;
			TopRight((RectTransform)pill.transform, 18, 14, 85, 36);

			WorldTabsView tabs = BuildTabs(safe);

			// Garden path.
			RectTransform scrollRect = NewUI("PathScroll", safe);
			Stretch(scrollRect, 0, 128, 0, 0);
			ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
			scroll.horizontal = false;
			scroll.vertical = true;
			scroll.movementType = ScrollRect.MovementType.Elastic;
			scroll.scrollSensitivity = 20;

			RectTransform viewport = NewUI("Viewport", scrollRect);
			Stretch(viewport);
			viewport.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0, 0, 0, -60);
			Image viewportHit = viewport.gameObject.AddComponent<Image>();
			viewportHit.color = Color.clear;

			RectTransform content = NewUI("Content", viewport);
			content.anchorMin = new Vector2(0, 0);
			content.anchorMax = new Vector2(1, 0);
			content.pivot = new Vector2(0.5f, 0);
			content.sizeDelta = new Vector2(0, 680);
			content.anchoredPosition = Vector2.zero;
			scroll.viewport = viewport;
			scroll.content = content;

			Vector2[] positions = new Vector2[FigmaCentres.Length];
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = new Vector2(FigmaCentres[i].x - 195f, PathBottomFigmaY - FigmaCentres[i].y);
			}

			PathLineGraphic band = AddPath(content, "PathBand", Theme.Get(ThemeColor.YellowShadow), 26f, 0f, 0f);
			PathLineGraphic dots = AddPath(content, "PathDots", Color.white, 7f, 7f, 9f);

			RectTransform nodes = NewUI("Nodes", content);
			Stretch(nodes);
			nodes.pivot = new Vector2(0.5f, 0);

			ToastView toast = Kit<ToastView>("Toast", root);

			LevelSelectScreenView view = root.gameObject.AddComponent<LevelSelectScreenView>();
			SetRefs(view,
				("backButton", back),
				("perfectsPill", pill),
				("worldTabs", tabs),
				("toast", toast),
				("scroll", scroll),
				("nodeContainer", nodes),
				("pathBand", band),
				("pathDots", dots),
				("nodePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(NodePath).GetComponent<LevelNodeView>()));

			SerializedObject so = new SerializedObject(view);
			SerializedProperty array = so.FindProperty("nodePositions");
			array.arraySize = positions.Length;
			for (int i = 0; i < positions.Length; i++)
			{
				array.GetArrayElementAtIndex(i).vector2Value = positions[i];
			}

			so.FindProperty("bottomPadding").floatValue = BottomPadding;
			so.ApplyModifiedPropertiesWithoutUndo();

			Vector2[] preview = new Vector2[positions.Length];
			for (int i = 0; i < preview.Length; i++)
			{
				preview[i] = positions[i] + Vector2.up * BottomPadding;
			}

			band.SetPoints(preview);
			dots.SetPoints(preview);

			Save(root.gameObject, ScreenPath);
		}

		private static PathLineGraphic AddPath(Transform parent, string name, Color color, float thickness, float dash, float gap)
		{
			RectTransform rect = NewUI(name, parent);
			Stretch(rect);
			rect.pivot = new Vector2(0.5f, 0);
			PathLineGraphic path = rect.gameObject.AddComponent<PathLineGraphic>();
			path.color = color;
			path.raycastTarget = false;
			SerializedObject so = new SerializedObject(path);
			so.FindProperty("thickness").floatValue = thickness;
			so.FindProperty("dashLength").floatValue = dash;
			so.FindProperty("gapLength").floatValue = gap;
			so.ApplyModifiedPropertiesWithoutUndo();
			return path;
		}

		private static WorldTabsView BuildTabs(Transform parent)
		{
			RectTransform track = NewUI("WorldTabs", parent);
			TopCentre(track, 80, 350, 40);
			Image trackImage = AddFrameLayer(track, "Track", "SegmentTrack", ThemeColor.Yellow, new Vector4(4, 4, 4, 4), themed: false);
			trackImage.color = TabTrack;

			HorizontalLayoutGroup row = track.gameObject.AddComponent<HorizontalLayoutGroup>();
			row.padding = new RectOffset(4, 4, 4, 4);
			row.spacing = 2;
			row.childAlignment = TextAnchor.MiddleCenter;
			row.childControlWidth = row.childControlHeight = true;
			row.childForceExpandWidth = false;
			row.childForceExpandHeight = true;

			string[] names = { "BACK GARDEN", "THE PARK", "SEASIDE" };
			float[] widths = { 124, 110, 104 };
			WorldTabsView.Tab[] tabs = new WorldTabsView.Tab[names.Length];

			for (int i = 0; i < names.Length; i++)
			{
				RectTransform tab = NewUI($"Tab{i}", track);
				tab.gameObject.AddComponent<LayoutElement>().preferredWidth = widths[i];

				Image highlight = AddFrameLayer(tab, "Highlight", "SegmentOn", ThemeColor.White, new Vector4(4, 4, 4, 4));

				HorizontalLayoutGroup content = tab.gameObject.AddComponent<HorizontalLayoutGroup>();
				content.spacing = 4;
				content.childAlignment = TextAnchor.MiddleCenter;
				content.childControlWidth = content.childControlHeight = true;
				content.childForceExpandWidth = content.childForceExpandHeight = false;

				Image lockIcon = AddIcon(tab, "Lock", "lock", 14, ThemeColor.Brown);
				Fixed(lockIcon.rectTransform, 14, 14);
				TextMeshProUGUI label = AddText(tab, "Label", names[i], ThemeFont.Heading, 16, ThemeColor.Ink, 4f, themedColor: false);

				Image hit = tab.gameObject.AddComponent<Image>();
				hit.color = Color.clear;
				Button button = tab.gameObject.AddComponent<Button>();
				button.transition = Selectable.Transition.None;
				button.targetGraphic = hit;
				SetRefs(tab.gameObject.AddComponent<PressScale>(), ("target", tab));

				tabs[i] = new WorldTabsView.Tab { button = button, highlight = highlight, label = label, lockIcon = lockIcon };
			}

			WorldTabsView view = track.gameObject.AddComponent<WorldTabsView>();
			SerializedObject so = new SerializedObject(view);
			so.FindProperty("theme").objectReferenceValue = Theme;
			SerializedProperty array = so.FindProperty("tabs");
			array.arraySize = tabs.Length;
			for (int i = 0; i < tabs.Length; i++)
			{
				SerializedProperty element = array.GetArrayElementAtIndex(i);
				element.FindPropertyRelative("button").objectReferenceValue = tabs[i].button;
				element.FindPropertyRelative("highlight").objectReferenceValue = tabs[i].highlight;
				element.FindPropertyRelative("label").objectReferenceValue = tabs[i].label;
				element.FindPropertyRelative("lockIcon").objectReferenceValue = tabs[i].lockIcon;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
			view.Show(Lawnmower.Presenters.LevelSelectPresenter.Worlds, 0);
			return view;
		}
	}
}
