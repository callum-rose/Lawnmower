using BunnyHeaven.UI;
using BunnyHeaven.UI.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Lawnmower.Screens.Editor.ScreenBuilderUtil;

namespace Lawnmower.Screens.Editor
{
	/// <summary>
	/// Builds the Settings overlay prefab (Figma 12:770) from the kit. <see cref="SettingsOverlay.Show"/> loads it from
	/// Resources. Re-run after a layout change; once it's hand-tweaked in the editor, edit it there instead.
	/// </summary>
	public static class SettingsBuilder
	{
		public const string OverlayPath = "Assets/Prefabs/UI/Settings/Resources/" + SettingsOverlay.ResourcePath + ".prefab";

		public const string Credits =
			"Sounds: Kenney (kenney.nl), CC0  ·  Fonts: Londrina Solid & Shantell Sans, SIL Open Font License 1.1";

		[MenuItem("Bunny Heaven/Screens/Build Settings Overlay")]
		public static void Build()
		{
			GameObject root = new GameObject("SettingsOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
				typeof(GraphicRaycaster), typeof(CanvasGroup)) { layer = 5 };
			Canvas canvas = root.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 500;
			LevelSelectBuilder.ConfigureScaler(root.GetComponent<CanvasScaler>());

			RectTransform screen = NewUI("Settings", root.transform);
			Stretch(screen);

			RectTransform background = (RectTransform)Kit<Transform>("ScreenBackground", screen);
			Stretch(background);
			Image blocker = background.gameObject.GetComponentInChildren<Image>();
			blocker.raycastTarget = true;

			RectTransform safe = NewUI("SafeArea", screen);
			Stretch(safe);
			safe.gameObject.AddComponent<SafeAreaPanel>();

			ButtonView back = AddBackButton(safe);
			TextMeshProUGUI title = AddText(safe, "Title", "SETTINGS", ThemeFont.Heading, 48, ThemeColor.White, 4f, inkShadow: true);
			TopCentre(title.rectTransform, 0, 260, 57);

			// The list scrolls on short screens; on a 390x844 frame it all fits.
			RectTransform scrollRect = NewUI("List", safe);
			Stretch(scrollRect, 0, 76, 0, 0);
			ScrollRect scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
			scroll.horizontal = false;
			scroll.movementType = ScrollRect.MovementType.Clamped;
			RectTransform viewport = NewUI("Viewport", scrollRect);
			Stretch(viewport);
			viewport.gameObject.AddComponent<RectMask2D>().padding = new Vector4(0, -20, 0, 0);
			viewport.gameObject.AddComponent<Image>().color = Color.clear;

			RectTransform content = NewUI("Content", viewport);
			content.anchorMin = new Vector2(0, 1);
			content.anchorMax = new Vector2(1, 1);
			content.pivot = new Vector2(0.5f, 1);
			content.sizeDelta = Vector2.zero;
			VerticalLayoutGroup column = content.gameObject.AddComponent<VerticalLayoutGroup>();
			column.padding = new RectOffset(24, 24, 0, 24);
			column.spacing = 9;
			column.childAlignment = TextAnchor.UpperCenter;
			column.childControlWidth = column.childControlHeight = true;
			column.childForceExpandWidth = true;
			column.childForceExpandHeight = false;
			content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			scroll.viewport = viewport;
			scroll.content = content;

			AddHeader(content, "DISPLAY");
			ListRowView graphicsRow = AddRow(content, "GraphicsRow", "GRAPHICS", "sparkle", ThemeColor.Blue);
			ToggleSegmentView graphics = AddSegment(graphicsRow, "LOW", "HIGH", 1);

			AddHeader(content, "CONTROLS");
			ListRowView movementRow = AddRow(content, "MovementRow", "MOVEMENT", "move", ThemeColor.Red);
			ToggleSegmentView movement = AddSegment(movementRow, "SWIPE", "ARROWS", 0);
			ListRowView hapticsRow = AddRow(content, "HapticsRow", "HAPTICS", "vibrate", ThemeColor.Green);
			ToggleSwitchView haptics = Kit<ToggleSwitchView>("ToggleSwitch", hapticsRow.Accessory);

			AddHeader(content, "AUDIO");
			ListRowView musicRow = AddRow(content, "MusicRow", "MUSIC", "music", ThemeColor.Blue);
			SliderView music = Kit<SliderView>("Slider", musicRow.Accessory);
			ListRowView soundsRow = AddRow(content, "SoundsRow", "SOUNDS", "sound", ThemeColor.Red);
			SliderView sounds = Kit<SliderView>("Slider", soundsRow.Accessory);

			AddHeader(content, "GAME");
			ListRowView tutorialRow = AddRow(content, "ReplayTutorialRow", "REPLAY TUTORIAL", "howtoplay", ThemeColor.Green, chevron: true);
			ListRowView resetRow = AddRow(content, "ResetProgressRow", "RESET PROGRESS", "trash", ThemeColor.Red, chevron: true);
			resetRow.SetTextColor(ThemeColor.Red);

			RectTransform spacer = NewUI("Spacer", content);
			spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 4;

			TextMeshProUGUI credits = AddText(content, "Credits", Credits, ThemeFont.Body, 12, ThemeColor.Brown, 0f);
			credits.textWrappingMode = TextWrappingModes.Normal;
			credits.lineSpacing = -10;
			TextMeshProUGUI version = AddText(content, "Version", "v0.1", ThemeFont.Body, 13, ThemeColor.Brown, 0f);

			ModalCardView modal = BuildResetModal(screen, out ButtonView confirm, out ButtonView cancel);
			ToastView toast = Kit<ToastView>("Toast", screen);

			SettingsView view = screen.gameObject.AddComponent<SettingsView>();
			SetRefs(view,
				("backButton", back),
				("graphics", graphics),
				("movement", movement),
				("haptics", haptics),
				("music", music),
				("sounds", sounds),
				("replayTutorial", tutorialRow),
				("resetProgress", resetRow),
				("resetModal", modal),
				("confirmReset", confirm),
				("cancelReset", cancel),
				("version", version),
				("toast", toast));

			SettingsOverlayView overlay = root.AddComponent<SettingsOverlayView>();
			SetRefs(overlay, ("view", view));

			Save(root, OverlayPath);
			AssetDatabase.SaveAssets();
		}

		private static void AddHeader(Transform parent, string text)
		{
			TextMeshProUGUI header = AddText(parent, $"Header_{text}", text, ThemeFont.Heading, 16, ThemeColor.Brown, 8f);
			header.alignment = TextAlignmentOptions.BottomLeft;
			header.gameObject.AddComponent<LayoutElement>().preferredHeight = 19;
		}

		private static ListRowView AddRow(Transform parent, string name, string label, string icon, ThemeColor chip, bool chevron = false)
		{
			ListRowView row = Kit<ListRowView>("ListRow", parent);
			row.name = name;
			row.SetLabel(label);
			row.SetIcon(KitAssets.Icon(icon));
			row.SetChipColor(chip);
			row.SetTextColor(ThemeColor.Ink);
			row.SetShowChevron(chevron);
			LayoutElement element = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
			element.preferredHeight = element.minHeight = 58;
			return row;
		}

		private static ToggleSegmentView AddSegment(ListRowView row, string left, string right, int selected)
		{
			ToggleSegmentView segment = Kit<ToggleSegmentView>("ToggleSegment", row.Accessory);
			segment.SetLabels(left, right);
			segment.SetSelectedWithoutNotify(selected);
			return segment;
		}

		private static ModalCardView BuildResetModal(Transform parent, out ButtonView confirm, out ButtonView cancel)
		{
			ModalCardView modal = Kit<ModalCardView>("ModalCard", parent);
			modal.name = "ResetConfirmation";
			Stretch((RectTransform)modal.transform);
			modal.SetTitle("RESET PROGRESS?");
			modal.Title.fontSize = 36;
			modal.SetMessage("Every level's best, perfect and unlock will be wiped. This can't be undone.");
			modal.Message.fontSize = 16;

			RectTransform row = NewUI("Buttons", modal.Content);
			HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 10;
			layout.childAlignment = TextAnchor.MiddleCenter;
			layout.childControlWidth = layout.childControlHeight = true;
			layout.childForceExpandWidth = layout.childForceExpandHeight = false;

			cancel = HalfButton(row, "BigButton_Blue", "CancelButton", "KEEP", null);
			confirm = HalfButton(row, "BigButton_Red", "ResetButton", "RESET", "trash");

			modal.gameObject.SetActive(false);
			return modal;
		}

		private static ButtonView HalfButton(Transform parent, string kit, string name, string label, string icon)
		{
			ButtonView button = Kit<ButtonView>(kit, parent);
			button.name = name;
			button.SetLabel(label);
			button.Label.fontSize = 22;
			button.SetIcon(icon != null ? KitAssets.Icon(icon) : null);
			if (button.Icon != null)
			{
				Fixed(button.Icon.rectTransform, 20, 20);
			}

			Fixed((RectTransform)button.transform, 132, 50);
			RectTransform content = (RectTransform)button.transform.Find("Content");
			Stretch(content, 10, 0, 10, 4);
			return button;
		}
	}
}
