using BunnyHeaven.UI;
using BunnyHeaven.UI.Editor;
using Game.UI.Screens;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lawnmower.Editor.GameScreens
{
	/// <summary>
	/// Builds Assets/Prefabs/Game/UI/GameScreen.prefab (HUD, Pause, Level Complete, Level Failed, tutorial card, on-screen
	/// arrows) from the Bunny Heaven kit prefabs, laid out from the Figma frames (390x844 points). The kit prefabs are
	/// nested, so kit changes flow through; only this prefab is regenerated.
	/// </summary>
	public static class GameScreenBuilder
	{
		public const string PrefabPath = "Assets/Prefabs/Game/UI/GameScreen.prefab";

		private const string KitDir = "Assets/Prefabs/UI/BunnyHeaven";
		private const float FrameWidth = 390;
		private const float FrameHeight = 844;

		// Figma frames include a mock 47pt status bar and a 34pt home indicator; the safe area replaces both.
		private const float SafeTop = 47;
		private const float SafeBottom = 810;

		private static readonly Vector4 Overhang = new(8, 8, 8, 12);

		private static UiTheme _theme;

		[MenuItem("Bunny Heaven/Game Screens/Rebuild Screen Prefab")]
		public static void BuildPrefab()
		{
			_theme = KitAssets.Theme;
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));

			GameObject root = BuildScreen();
			PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
			Object.DestroyImmediate(root);
			AssetDatabase.SaveAssets();
		}

		// ---------- Screen ----------

		private static GameObject BuildScreen()
		{
			var root = new GameObject("GameScreen", typeof(RectTransform)) { layer = 5 };
			var canvas = root.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 10;
			var scaler = root.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(FrameWidth, FrameHeight);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
			root.AddComponent<GraphicRaycaster>();
			var rootRect = (RectTransform)root.transform;

			RectTransform safe = NewUI("SafeArea", rootRect);
			Stretch(safe, 0, 0, 0, 0);
			safe.gameObject.AddComponent<Lawnmower.Screens.SafeAreaPanel>().SetMinInsets(0f, 0f);

			GameHudView hud = BuildHud(safe);
			ArrowPadView arrows = BuildArrowPad(safe);
			TutorialOverlayView tutorial = BuildTutorial(safe);
			LevelFailedSheetView failed = BuildFailedSheet(rootRect);
			PauseCardView pause = BuildPauseCard(rootRect);
			LevelCompleteCardView complete = BuildCompleteCard(rootRect);

			var toast = Kit("Toast", rootRect).GetComponent<ToastView>();
			var toastRect = (RectTransform)toast.transform;
			toastRect.anchoredPosition = new Vector2(0, 300);

			var view = root.AddComponent<GameScreenView>();
			SetRefs(view,
				("hud", hud),
				("pauseCard", pause),
				("completeCard", complete),
				("failedSheet", failed),
				("arrowPad", arrows),
				("tutorial", tutorial),
				("toast", toast));
			return root;
		}

		// ---------- HUD (12:330) ----------

		private static GameHudView BuildHud(RectTransform parent)
		{
			RectTransform hudRect = NewUI("Hud", parent);
			Stretch(hudRect, 0, 0, 0, 0);

			RectTransform topBar = NewUI("TopBar", hudRect);
			topBar.anchorMin = new Vector2(0, 1);
			topBar.anchorMax = new Vector2(1, 1);
			topBar.pivot = new Vector2(0.5f, 1);
			topBar.sizeDelta = new Vector2(0, 52);
			topBar.anchoredPosition = new Vector2(0, -(54 - SafeTop));

			ButtonView pause = RoundButton(topBar, "PauseButton", ButtonColor.WhiteOnTeal, "pause", null, 52, 23, 0);
			var pauseRect = (RectTransform)pause.transform;
			pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(0, 1);
			pauseRect.pivot = new Vector2(0, 1);
			pauseRect.anchoredPosition = new Vector2(18, 0);

			PillView level = Pill(topBar, "LevelPill", ThemeColor.Green, "flag", "LEVEL 1", 24);
			var levelRect = (RectTransform)level.transform;
			levelRect.anchorMin = levelRect.anchorMax = new Vector2(0, 1);
			levelRect.pivot = new Vector2(0, 1);
			levelRect.anchoredPosition = new Vector2(82, -4);

			PillView progress = Pill(topBar, "ProgressPill", ThemeColor.Blue, "check", "0/7", 22);
			var progressRect = (RectTransform)progress.transform;
			progressRect.anchorMin = progressRect.anchorMax = new Vector2(1, 1);
			progressRect.pivot = new Vector2(1, 1);
			progressRect.anchoredPosition = new Vector2(-18, -4);

			// Controls: captions share a baseline; Undo is the big primary button in the middle.
			RectTransform controls = NewUI("Controls", hudRect);
			controls.anchorMin = controls.anchorMax = new Vector2(0.5f, 0);
			controls.pivot = new Vector2(0.5f, 0);
			controls.sizeDelta = new Vector2(252, 111);
			controls.anchoredPosition = new Vector2(0, 0);

			ButtonView restart = RoundButton(controls, "RestartButton", ButtonColor.Blue, "restart", "RESTART", 58, 26, 16);
			ButtonView undo = RoundButton(controls, "UndoButton", ButtonColor.Yellow, "undo", "UNDO", 84, 37, 18);
			ButtonView redo = RoundButton(controls, "RedoButton", ButtonColor.Blue, "redo", "REDO", 58, 26, 16);
			BottomAt(restart, -97);
			BottomAt(undo, 0);
			BottomAt(redo, 97);
			undo.gameObject.AddComponent<CanvasGroup>();
			redo.gameObject.AddComponent<CanvasGroup>();

			var hud = hudRect.gameObject.AddComponent<GameHudView>();
			SetRefs(hud,
				("pauseButton", pause),
				("levelPill", level),
				("progressPill", progress),
				("restartButton", restart),
				("undoButton", undo),
				("redoButton", redo),
				("topBar", topBar),
				("controls", controls));
			return hud;
		}

		private static void BottomAt(ButtonView button, float x)
		{
			var rect = (RectTransform)button.transform;
			rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
			rect.pivot = new Vector2(0.5f, 0);
			rect.anchoredPosition = new Vector2(x, 0);
		}

		/// <summary>The Arrows scheme's on-screen arrows: a cross of white kit buttons above the controls.</summary>
		private static ArrowPadView BuildArrowPad(RectTransform parent)
		{
			RectTransform pad = NewUI("ArrowPad", parent);
			pad.anchorMin = pad.anchorMax = new Vector2(0.5f, 0);
			pad.pivot = new Vector2(0.5f, 0);
			pad.sizeDelta = new Vector2(176, 168);
			pad.anchoredPosition = new Vector2(0, 131);

			const float offset = 58;
			ButtonView Arrow(string name, float angle, Vector2 position)
			{
				ButtonView button = RoundButton(pad, name, ButtonColor.WhiteOnTeal, "next", null, 54, 26, 0);
				button.Icon.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
				var rect = (RectTransform)button.transform;
				rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
				rect.anchoredPosition = position;
				return button;
			}

			ButtonView up = Arrow("Up", 90, new Vector2(0, offset));
			ButtonView down = Arrow("Down", -90, new Vector2(0, -offset));
			ButtonView left = Arrow("Left", 180, new Vector2(-offset, 0));
			ButtonView right = Arrow("Right", 0, new Vector2(offset, 0));

			var view = pad.gameObject.AddComponent<ArrowPadView>();
			SetRefs(view, ("up", up), ("down", down), ("left", left), ("right", right));
			pad.gameObject.SetActive(false);
			return view;
		}

		/// <summary>The tutorial hint: a white card with a green icon chip, just under the top bar. Never blocks touches.</summary>
		private static TutorialOverlayView BuildTutorial(RectTransform parent)
		{
			RectTransform root = NewUI("Tutorial", parent);
			root.anchorMin = new Vector2(0, 1);
			root.anchorMax = new Vector2(1, 1);
			root.pivot = new Vector2(0.5f, 1);
			root.sizeDelta = new Vector2(0, 64);
			root.anchoredPosition = new Vector2(0, -(118 - SafeTop));
			var group = root.gameObject.AddComponent<CanvasGroup>();
			group.blocksRaycasts = false;
			group.interactable = false;

			RectTransform card = Kit("Card", root);
			card.name = "Card";
			card.anchorMin = card.anchorMax = new Vector2(0.5f, 1);
			card.pivot = new Vector2(0.5f, 1);
			card.sizeDelta = new Vector2(342, 64);
			card.anchoredPosition = Vector2.zero;
			card.GetComponent<FrameView>().SetShadow(ThemeColor.TealShadow);
			foreach (Graphic graphic in card.GetComponentsInChildren<Graphic>(true))
			{
				graphic.raycastTarget = false;
			}

			RectTransform content = (RectTransform)card.Find("Content");
			var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
			row.spacing = 12;
			row.padding = new RectOffset(-6, 0, 0, 0);
			row.childAlignment = TextAnchor.MiddleLeft;
			row.childControlWidth = row.childControlHeight = true;
			row.childForceExpandWidth = row.childForceExpandHeight = false;

			Image icon = Chip(content, 40, ThemeColor.Green, "move", 22);
			TextMeshProUGUI message = Text(content, "Message", "Swipe on the lawn to move the bunny", ThemeFont.BodyBold, 17, 0,
				ThemeColor.Ink);
			message.alignment = TextAlignmentOptions.MidlineLeft;
			message.textWrappingMode = TextWrappingModes.Normal;
			message.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

			var view = root.gameObject.AddComponent<TutorialOverlayView>();
			SetRefs(view,
				("card", card),
				("message", message),
				("icon", icon),
				("moveIcon", KitAssets.Icon("move")),
				("mowIcon", KitAssets.Icon("sparkle")));
			return view;
		}

		// ---------- Pause (12:531) ----------

		private static PauseCardView BuildPauseCard(RectTransform parent)
		{
			RectTransform modal = Kit("ModalCard", parent);
			modal.name = "PauseCard";
			Stretch(modal, 0, 0, 0, 0);
			var modalView = modal.GetComponent<ModalCardView>();
			RectTransform card = (RectTransform)modal.Find("Card");
			var column = card.GetComponent<VerticalLayoutGroup>();
			column.padding = new RectOffset(22, 22, 44, 24);
			column.spacing = 12;

			modalView.Title.text = "PAUSED";
			modalView.Title.fontSize = 52;
			modalView.Title.characterSpacing = 3;
			modalView.Message.fontSize = 15;
			modalView.Message.text = "Level 1 · 0 of 10 perfect";

			// The badge sits over the card's top edge.
			RectTransform badge = NewUI("Badge", card);
			badge.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
			badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1);
			badge.pivot = new Vector2(0.5f, 0.5f);
			badge.sizeDelta = new Vector2(70, 70);
			badge.anchoredPosition = new Vector2(0, 2);
			Frame(badge, "Circle", ThemeColor.Red, ThemeColor.RedShadow, sliced: false);
			Icon(badge, "pause", 30, ThemeColor.White);

			ButtonView resume = BigButton(modalView.Content, "ResumeButton", "BigButton_Green", "play", "RESUME", 266, 66, 28, 24);

			RectTransform row = NewUI("Buttons", modalView.Content);
			var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
			rowLayout.spacing = 6;
			rowLayout.childAlignment = TextAnchor.UpperCenter;
			rowLayout.childControlWidth = rowLayout.childControlHeight = true;
			rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;

			ButtonView restart = RoundButton(row, "RestartButton", ButtonColor.Blue, "restart", "RESTART", 52, 23, 14);
			ButtonView settings = RoundButton(row, "SettingsButton", ButtonColor.Yellow, "settings", "SETTINGS", 52, 23, 14);
			ButtonView levels = RoundButton(row, "LevelsButton", ButtonColor.Green, "levels", "LEVELS", 52, 23, 14);
			ButtonView menu = RoundButton(row, "MenuButton", ButtonColor.Red, "menu", "MENU", 52, 23, 14);
			foreach (ButtonView button in new[] { restart, settings, levels, menu })
			{
				button.gameObject.AddComponent<LayoutElement>().preferredWidth = 62;
			}

			var view = modal.gameObject.AddComponent<PauseCardView>();
			SetRefs(view,
				("modal", modalView),
				("subtitle", modalView.Message),
				("resumeButton", resume),
				("restartButton", restart),
				("settingsButton", settings),
				("levelsButton", levels),
				("menuButton", menu));
			return view;
		}

		// ---------- Level Complete (12:910) ----------

		private static LevelCompleteCardView BuildCompleteCard(RectTransform parent)
		{
			RectTransform modal = Kit("ModalCard", parent);
			modal.name = "CompleteCard";
			Stretch(modal, 0, 0, 0, 0);
			var modalView = modal.GetComponent<ModalCardView>();
			RectTransform card = (RectTransform)modal.Find("Card");
			var column = card.GetComponent<VerticalLayoutGroup>();
			column.padding = new RectOffset(14, 14, 14, 22);
			column.spacing = 14;
			modalView.Title.gameObject.SetActive(false);
			modalView.Message.gameObject.SetActive(false);

			RectTransform content = modalView.Content;
			content.GetComponent<VerticalLayoutGroup>().spacing = 14;

			// Banner: kicker, headline, badge (perfect) or par line, level.
			RectTransform banner = NewUI("Banner", content);
			Fixed(banner, 282, 250);
			FrameView bannerFrame = Frame(banner, "Banner", ThemeColor.Green, null, sliced: true, shadow: false, overhang: new Vector4(8, 8, 8, 8));
			var bannerLayout = banner.gameObject.AddComponent<VerticalLayoutGroup>();
			bannerLayout.padding = new RectOffset(0, 0, 16, 16);
			bannerLayout.spacing = 2;
			bannerLayout.childAlignment = TextAnchor.UpperCenter;
			bannerLayout.childControlWidth = bannerLayout.childControlHeight = true;
			bannerLayout.childForceExpandWidth = true;
			bannerLayout.childForceExpandHeight = false;

			Text(banner, "Kicker", "LAWN", ThemeFont.Heading, 30, 8, ThemeColor.White);
			TextMeshProUGUI headline = Text(banner, "Headline", "PERFECT!", ThemeFont.Heading, 60, 3, ThemeColor.White, inkShadow: true);

			RectTransform badgeSlot = NewUI("BadgeSlot", banner);
			Fixed(badgeSlot, 70, 76);
			RectTransform badge = NewUI("Badge", badgeSlot);
			badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 0.5f);
			badge.sizeDelta = new Vector2(70, 70);
			Frame(badge, "Circle", ThemeColor.Yellow, ThemeColor.YellowShadow, sliced: false);
			Icon(badge, "check", 36, ThemeColor.Ink);
			TextMeshProUGUI parLine = Text(badgeSlot, "ParLine", "Par: 7 moves", ThemeFont.BodyBold, 18, 0, ThemeColor.GreenPale);
			Stretch(parLine.rectTransform, -40, 0, -40, 0);
			parLine.gameObject.SetActive(false);

			TextMeshProUGUI levelLine = Text(banner, "Level", "LEVEL 1", ThemeFont.Heading, 22, 6, ThemeColor.GreenPale);

			// Stats.
			RectTransform stats = NewUI("Stats", content);
			var statsLayout = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
			statsLayout.spacing = 22;
			statsLayout.childAlignment = TextAnchor.UpperCenter;
			statsLayout.childControlWidth = statsLayout.childControlHeight = true;
			statsLayout.childForceExpandWidth = statsLayout.childForceExpandHeight = false;
			TextMeshProUGUI moves = Stat(stats, "Moves", "MOVES");
			TextMeshProUGUI undos = Stat(stats, "Undos", "UNDOS");
			TextMeshProUGUI time = Stat(stats, "Time", "TIME");

			TextMeshProUGUI best = Text(content, "BestLine", "New best: 7 moves!", ThemeFont.BodyBold, 15, 0, ThemeColor.Red);

			ButtonView next = BigButton(content, "NextButton", "BigButton_Green", "next", "NEXT LEVEL", 282, 66, 28, 24);

			RectTransform row = NewUI("Buttons", content);
			var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
			rowLayout.spacing = 10;
			rowLayout.childAlignment = TextAnchor.MiddleCenter;
			rowLayout.childControlWidth = rowLayout.childControlHeight = true;
			rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
			ButtonView replay = BigButton(row, "ReplayButton", "BigButton_Blue", "restart", "REPLAY", 136, 50, 20, 20);
			ButtonView levels = BigButton(row, "LevelsButton", "BigButton_Red", "levels", "LEVELS", 136, 50, 20, 20);

			var view = modal.gameObject.AddComponent<LevelCompleteCardView>();
			SetRefs(view,
				("modal", modalView),
				("banner", bannerFrame),
				("headline", headline),
				("badge", badge.gameObject),
				("parLine", parLine),
				("levelLine", levelLine),
				("movesValue", moves),
				("undosValue", undos),
				("timeValue", time),
				("bestLine", best),
				("bestLineColor", best.GetComponent<ThemedGraphic>()),
				("nextButton", next),
				("replayButton", replay),
				("levelsButton", levels));
			return view;
		}

		private static TextMeshProUGUI Stat(RectTransform parent, string name, string label)
		{
			RectTransform column = NewUI(name, parent);
			var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
			layout.spacing = 4;
			layout.childAlignment = TextAnchor.UpperCenter;
			layout.childControlWidth = layout.childControlHeight = true;
			layout.childForceExpandWidth = layout.childForceExpandHeight = false;

			RectTransform circle = NewUI("Circle", column);
			Fixed(circle, 62, 62);
			Frame(circle, "StatCircle", ThemeColor.Cream, null, sliced: false, shadow: false, overhang: new Vector4(8, 8, 8, 8));
			TextMeshProUGUI value = Text(circle, "Value", "0", ThemeFont.Heading, 26, 0, ThemeColor.Ink);
			Stretch(value.rectTransform, 0, 0, 0, 0);
			Text(column, "Label", label, ThemeFont.Heading, 14, 5, ThemeColor.Muted);
			return value;
		}

		// ---------- Level Failed (12:1106) ----------

		private static LevelFailedSheetView BuildFailedSheet(RectTransform parent)
		{
			RectTransform root = NewUI("FailedSheet", parent);
			Stretch(root, 0, 0, 0, 0);
			root.gameObject.AddComponent<CanvasGroup>();

			// Covers the board and the HUD: only the sheet's buttons work while it's up.
			var blocker = root.gameObject.AddComponent<Image>();
			blocker.color = Color.clear;
			blocker.raycastTarget = true;

			// Annotation: "too short!" with an arrow down to the failed tile, placed over the mower each frame.
			RectTransform annotation = NewUI("Annotation", root);
			annotation.anchorMin = annotation.anchorMax = new Vector2(0.5f, 0.5f);
			annotation.pivot = new Vector2(0.5f, 0);
			annotation.sizeDelta = new Vector2(10, 10);
			Image arrow = Icon(annotation, "next", 30, ThemeColor.White);
			arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
			arrow.rectTransform.anchoredPosition = new Vector2(0, 84);
			TextMeshProUGUI note = Text(annotation, "Note", "too short!", ThemeFont.BodyBold, 22, 0, ThemeColor.White);
			note.rectTransform.sizeDelta = new Vector2(140, 30);
			note.rectTransform.anchoredPosition = new Vector2(46, 118);
			note.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
			note.fontStyle = FontStyles.Normal;
			note.outlineWidth = 0;

			// The sheet slides up from the bottom; its rect includes the tag straddling its top edge.
			RectTransform sheet = NewUI("Sheet", root);
			sheet.anchorMin = new Vector2(0, 0);
			sheet.anchorMax = new Vector2(1, 0);
			sheet.pivot = new Vector2(0.5f, 0);
			sheet.sizeDelta = new Vector2(0, (FrameHeight - 582) - (FrameHeight - SafeBottom) + 28 + 34);
			sheet.anchoredPosition = Vector2.zero;

			RectTransform body = NewUI("Body", sheet);
			Stretch(body, -2, 28, -2, 0);
			Frame(body, "ModalCard", ThemeColor.White, ThemeColor.InkShadow, sliced: true, overhang: new Vector4(8, 8, 8, 40));
			var bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
			bodyLayout.padding = new RectOffset(26, 26, 44, 34);
			bodyLayout.spacing = 14;
			bodyLayout.childAlignment = TextAnchor.UpperCenter;
			bodyLayout.childControlWidth = bodyLayout.childControlHeight = true;
			bodyLayout.childForceExpandWidth = false;
			bodyLayout.childForceExpandHeight = false;

			TextMeshProUGUI hint = Text(body, "Hint", "You mowed a finished patch down to the dirt. Undo and try another route.",
				ThemeFont.Body, 16, 0, ThemeColor.Ink);
			hint.textWrappingMode = TextWrappingModes.Normal;
			hint.gameObject.AddComponent<LayoutElement>().preferredWidth = 320;

			ButtonView undo = BigButton(body, "UndoLastMoveButton", "BigButton_Yellow", "undo", "UNDO LAST MOVE", 342, 66, 28, 24);

			RectTransform row = NewUI("Buttons", body);
			var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
			rowLayout.spacing = 10;
			rowLayout.childAlignment = TextAnchor.MiddleCenter;
			rowLayout.childControlWidth = rowLayout.childControlHeight = true;
			rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = false;
			ButtonView restart = BigButton(row, "RestartButton", "BigButton_Blue", "restart", "RESTART", 166, 50, 20, 20);
			ButtonView levels = BigButton(row, "LevelsButton", "BigButton_Red", "levels", "LEVELS", 166, 50, 20, 20);

			RectTransform tag = NewUI("Tag", sheet);
			tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 1);
			tag.pivot = new Vector2(0.5f, 1);
			tag.sizeDelta = new Vector2(196, 65);
			tag.anchoredPosition = Vector2.zero;
			tag.localRotation = Quaternion.Euler(0, 0, -3);
			Frame(tag, "Ribbon", ThemeColor.Red, ThemeColor.RedShadow, sliced: true);
			TextMeshProUGUI tagText = Text(tag, "Label", "SCALPED!", ThemeFont.Heading, 40, 4, ThemeColor.White);
			Stretch(tagText.rectTransform, 0, 0, 0, 4);

			var view = root.gameObject.AddComponent<LevelFailedSheetView>();
			SetRefs(view,
				("sheet", sheet),
				("undoButton", undo),
				("restartButton", restart),
				("levelsButton", levels),
				("annotation", annotation),
				("annotationNote", note.rectTransform));
			return view;
		}

		// ---------- Kit pieces ----------

		private static ButtonView RoundButton(RectTransform parent, string name, ButtonColor color, string icon, string label,
			float circle, float iconSize, float labelSize)
		{
			var button = Kit("RoundIconButton", parent).GetComponent<ButtonView>();
			button.name = name;
			button.SetColor(color);
			button.SetIcon(KitAssets.Icon(icon));
			button.Icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);

			var circleRect = (RectTransform)button.transform.Find("Circle");
			circleRect.sizeDelta = new Vector2(circle, circle);
			var element = circleRect.GetComponent<LayoutElement>();
			element.minWidth = element.preferredWidth = circle;
			element.minHeight = element.preferredHeight = circle;

			if (string.IsNullOrEmpty(label))
			{
				button.Label.gameObject.SetActive(false);
			}
			else
			{
				button.SetLabel(label);
				button.Label.fontSize = labelSize;
			}

			RecordAll(button.gameObject);
			return button;
		}

		private static ButtonView BigButton(RectTransform parent, string name, string prefab, string icon, string label,
			float width, float height, float labelSize, float iconSize)
		{
			var button = Kit(prefab, parent).GetComponent<ButtonView>();
			button.name = name;
			button.SetIcon(KitAssets.Icon(icon));
			button.SetLabel(label);
			button.Label.fontSize = labelSize;
			Fixed(button.Icon.rectTransform, iconSize, iconSize);
			Fixed((RectTransform)button.transform, width, height);
			RecordAll(button.gameObject);
			return button;
		}

		private static PillView Pill(RectTransform parent, string name, ThemeColor chip, string icon, string text, float size)
		{
			var pill = Kit("Pill", parent).GetComponent<PillView>();
			pill.name = name;
			pill.SetChipColor(chip);
			pill.SetIcon(KitAssets.Icon(icon));
			pill.SetText(text);
			pill.SetShadow(ThemeColor.TealShadow);
			pill.Label.fontSize = size;
			((RectTransform)pill.transform).sizeDelta = new Vector2(144, 48);
			RecordAll(pill.gameObject);
			return pill;
		}

		private static Image Chip(RectTransform parent, float size, ThemeColor color, string icon, float iconSize)
		{
			RectTransform chip = NewUI("Chip", parent);
			Fixed(chip, size, size);
			Frame(chip, "Chip", color, null, sliced: false, shadow: false, overhang: new Vector4(8, 8, 8, 8) * (size / 40f));
			return Icon(chip, icon, iconSize, ThemeColor.White);
		}

		private static Image Icon(RectTransform parent, string icon, float size, ThemeColor token)
		{
			RectTransform rect = NewUI("Icon", parent);
			rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(size, size);
			var image = rect.gameObject.AddComponent<Image>();
			image.sprite = KitAssets.Icon(icon);
			image.preserveAspect = true;
			image.raycastTarget = false;
			Themed(rect.gameObject, token);
			return image;
		}

		// ---------- Helpers ----------

		private static RectTransform Kit(string prefab, RectTransform parent)
		{
			var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitDir}/{prefab}.prefab");
			var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
			return (RectTransform)go.transform;
		}

		/// <summary>Kit setters change nested-prefab instances; record them as overrides so the saved prefab keeps them.</summary>
		private static void RecordAll(GameObject go)
		{
			foreach (Component component in go.GetComponentsInChildren<Component>(true))
			{
				if (component != null)
				{
					PrefabUtility.RecordPrefabInstancePropertyModifications(component);
				}
			}
		}

		private static RectTransform NewUI(string name, Transform parent)
		{
			var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
			var rect = (RectTransform)go.transform;
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

		private static void Fixed(RectTransform rect, float width, float height)
		{
			var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
			element.minWidth = element.preferredWidth = width;
			element.minHeight = element.preferredHeight = height;
			rect.sizeDelta = new Vector2(width, height);
		}

		/// <summary>A kit-style hand-drawn frame (tinted body over an offset shadow), as the kit builds them.</summary>
		private static FrameView Frame(RectTransform parent, string frame, ThemeColor fill, ThemeColor? shadowColor, bool sliced,
			bool shadow = true, Vector4? overhang = null)
		{
			Vector4 over = overhang ?? Overhang;
			Image fillImage = FrameLayer(parent, "FrameFill", $"Frames/Frame_{frame}", fill, sliced, over);
			Image shadowImage = null;
			if (shadow)
			{
				shadowImage = FrameLayer(parent, "FrameShadow", $"Frames/Frame_{frame}_Shadow", shadowColor ?? ThemeColor.YellowShadow,
					sliced, over);
			}

			// Shadow at the back, then the fill, then the content.
			fillImage.transform.SetAsFirstSibling();
			if (shadowImage != null)
			{
				shadowImage.transform.SetAsFirstSibling();
			}

			var view = parent.gameObject.AddComponent<FrameView>();
			SetRefs(view, ("fill", fillImage.GetComponent<ThemedGraphic>()),
				("shadow", shadowImage != null ? shadowImage.GetComponent<ThemedGraphic>() : null));
			return view;
		}

		private static Image FrameLayer(RectTransform parent, string name, string sprite, ThemeColor token, bool sliced, Vector4 overhang)
		{
			RectTransform rect = NewUI(name, parent);
			Stretch(rect, -overhang.x, -overhang.y, -overhang.z, -overhang.w);
			rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
			var image = rect.gameObject.AddComponent<Image>();
			image.sprite = KitAssets.Sprite(sprite);
			if (sliced)
			{
				image.type = Image.Type.Sliced;
				image.pixelsPerUnitMultiplier = 1f;
			}

			image.raycastTarget = false;
			Themed(rect.gameObject, token);
			return image;
		}

		private static TextMeshProUGUI Text(RectTransform parent, string name, string text, ThemeFont font, float size,
			float spacingPercent, ThemeColor color, bool inkShadow = false)
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
			themedText.Font = font;
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
				SerializedProperty property = so.FindProperty(field);
				if (property == null)
				{
					throw new System.ArgumentException($"{target.GetType().Name} has no field '{field}'");
				}

				property.objectReferenceValue = value;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
		}
	}
}
