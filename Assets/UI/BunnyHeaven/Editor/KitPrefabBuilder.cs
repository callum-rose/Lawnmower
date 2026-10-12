using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI.Editor
{
    /// <summary>
    /// Builds the Bunny Heaven UI kit prefabs from code, so they can be regenerated after the sprites,
    /// fonts or theme change. Sizes and spacing come from the Figma file (390x844 frames, points).
    /// </summary>
    public static class KitPrefabBuilder
    {
        public const string PrefabDir = "Assets/Prefabs/UI/BunnyHeaven";

        private static UiTheme _theme;

        // How far each frame sprite overhangs its element: left, top, right, bottom (Figma points).
        // The drawings include the wobble and the drop shadow, which sits 4pt lower.
        private static readonly Vector4 Overhang = new(8, 8, 8, 12);
        private static readonly Vector4 ChipOverhang = new(8, 8, 8, 8);

        [MenuItem("Bunny Heaven/UI Kit/Rebuild Everything")]
        public static void RebuildEverything()
        {
            KitAssets.ConfigureSprites();
            KitAssets.CreateFonts();
            KitAssets.CreateOrUpdateTheme();
            BuildAll();
        }

        [MenuItem("Bunny Heaven/UI Kit/Rebuild Prefabs")]
        public static void BuildAll()
        {
            _theme = KitAssets.CreateOrUpdateTheme();
            Directory.CreateDirectory(PrefabDir);

            Save(BuildCard(), "Card");
            Save(BuildPill(), "Pill");
            Save(BuildRoundIconButton(), "RoundIconButton");
            var bigButton = Save(BuildBigButton(), "BigButton_Green");
            SaveButtonVariant(bigButton, "BigButton_Yellow", ButtonColor.Yellow);
            SaveButtonVariant(bigButton, "BigButton_Blue", ButtonColor.Blue);
            SaveButtonVariant(bigButton, "BigButton_Red", ButtonColor.Red);
            Save(BuildToggleSegment(), "ToggleSegment");
            Save(BuildToggleSwitch(), "ToggleSwitch");
            Save(BuildSlider(), "Slider");
            Save(BuildListRow(), "ListRow");
            Save(BuildModalCard(), "ModalCard");
            Save(BuildToast(), "Toast");
            Save(BuildScreenBackground(), "ScreenBackground");
            AssetDatabase.SaveAssets();
        }

        // ---------- Prefabs ----------

        private static GameObject BuildCard()
        {
            var root = NewUI("Card", null, new Vector2(342, 58));
            AddFrame(root, "Card", ThemeColor.White, ThemeColor.YellowShadow, Overhang);
            var content = NewUI("Content", root);
            Stretch(content, 14, 10, 14, 10);
            return root.gameObject;
        }

        private static GameObject BuildPill()
        {
            var root = NewUI("Pill", null, new Vector2(144, 48));
            var frame = AddFrame(root, "Pill", ThemeColor.White, ThemeColor.TealShadow, Overhang);
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 18, 6, 6);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            SetControl(layout);
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var chip = AddChip(root, 36, ThemeColor.Green, KitAssets.Icon("flag"), 18, out var icon);
            var label = AddText(root, "Label", "LEVEL 7", ThemeFont.Heading, 24, ThemeColor.Ink, 3f);

            var view = root.gameObject.AddComponent<PillView>();
            SetRefs(view, ("frame", frame), ("chip", chip), ("icon", icon), ("label", label));
            return root.gameObject;
        }

        private static GameObject BuildRoundIconButton()
        {
            var root = NewUI("RoundIconButton", null, new Vector2(96, 92));
            var column = root.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6;
            column.childAlignment = TextAnchor.UpperCenter;
            SetControl(column);
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var circle = NewUI("Circle", root, new Vector2(64, 64));
            Fixed(circle, 64, 64);
            var frame = AddFrame(circle, "Circle", ThemeColor.Blue, ThemeColor.BlueShadow, Overhang);
            var icon = AddIcon(circle, "Icon", KitAssets.Icon("levels"), 28, ThemeColor.White);
            var label = AddText(root, "Label", "LEVELS", ThemeFont.Heading, 16, ThemeColor.Ink, 4f);

            var button = AddButton(root.gameObject, frame.Fill.Graphic, circle);
            var view = root.gameObject.AddComponent<ButtonView>();
            SetValue(view, "tintLabel", false);
            SetValue(view, "color", (int)ButtonColor.Blue);
            SetRefs(view, ("frame", frame), ("icon", icon), ("label", label));
            return button.gameObject;
        }

        private static GameObject BuildBigButton()
        {
            var root = NewUI("BigButton", null, new Vector2(300, 70));
            var frame = AddFrame(root, "Button", ThemeColor.Green, ThemeColor.GreenShadow, Overhang);
            var content = NewUI("Content", root);
            Stretch(content, 24, 0, 24, 4);
            var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10;
            row.childAlignment = TextAnchor.MiddleCenter;
            SetControl(row);
            var icon = AddIcon(content, "Icon", KitAssets.Icon("play"), 24, ThemeColor.White);
            Fixed(icon.rectTransform, 24, 24);
            var label = AddText(content, "Label", "PLAY", ThemeFont.Heading, 30, ThemeColor.White, 3f);

            var button = AddButton(root.gameObject, frame.Fill.Graphic, root);
            var view = root.gameObject.AddComponent<ButtonView>();
            SetValue(view, "color", (int)ButtonColor.Green);
            SetRefs(view, ("frame", frame), ("icon", icon), ("label", label));
            return button.gameObject;
        }

        private static void SaveButtonVariant(GameObject basePrefab, string name, ButtonColor color)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = name;
            var view = instance.GetComponent<ButtonView>();
            view.SetColor(color);
            foreach (var themed in instance.GetComponentsInChildren<ThemedGraphic>(true))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(themed);
                PrefabUtility.RecordPrefabInstancePropertyModifications(themed.Graphic);
            }
            SetValue(view, "color", (int)color);
            PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(instance);
        }

        private static GameObject BuildToggleSegment()
        {
            var root = NewUI("ToggleSegment", null, new Vector2(144, 33));
            AddFrame(root, "SegmentTrack", ThemeColor.SegmentTrack, null, ChipOverhang, shadow: false);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(3, 3, 3, 3);
            row.spacing = 2;
            row.childAlignment = TextAnchor.MiddleCenter;
            SetControl(row);
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var segments = new ToggleSegmentView.Segment[2];
            var labels = new[] { "LOW", "HIGH" };
            for (var i = 0; i < 2; i++)
            {
                var seg = NewUI($"Segment{i}", root);
                var segLayout = seg.gameObject.AddComponent<HorizontalLayoutGroup>();
                segLayout.padding = new RectOffset(12, 12, 4, 4);
                segLayout.childAlignment = TextAnchor.MiddleCenter;
                SetControl(segLayout);
                var highlightFrame = AddFrame(seg, "SegmentOn", ThemeColor.Green, null, ChipOverhang, shadow: false);
                var label = AddText(seg, "Label", labels[i], ThemeFont.Heading, 16, ThemeColor.Brown, 4f);
                var hit = seg.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                var button = seg.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                var selected = i == 1;
                highlightFrame.Fill.Graphic.enabled = selected;
                label.GetComponent<ThemedGraphic>().Token = selected ? ThemeColor.White : ThemeColor.Brown;
                segments[i] = new ToggleSegmentView.Segment
                {
                    button = button,
                    highlight = (Image)highlightFrame.Fill.Graphic,
                    label = label,
                };
            }

            var view = root.gameObject.AddComponent<ToggleSegmentView>();
            var so = new SerializedObject(view);
            var array = so.FindProperty("segments");
            array.arraySize = segments.Length;
            for (var i = 0; i < segments.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("button").objectReferenceValue = segments[i].button;
                element.FindPropertyRelative("highlight").objectReferenceValue = segments[i].highlight;
                element.FindPropertyRelative("label").objectReferenceValue = segments[i].label;
            }

            so.FindProperty("theme").objectReferenceValue = _theme;
            so.FindProperty("selectedIndex").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root.gameObject;
        }

        private static GameObject BuildToggleSwitch()
        {
            var root = NewUI("ToggleSwitch", null, new Vector2(56, 32));
            Fixed(root, 56, 32);
            var track = AddImage(root, "Track", KitAssets.Sprite("Frames/Toggle_Track"), ThemeColor.Green, true);
            Stretch(track.rectTransform, -4, -4, -4, -4);
            var knobArea = NewUI("KnobArea", root);
            Stretch(knobArea, 4, 4, 4, 4);
            var knob = AddImage(knobArea, "Knob", KitAssets.Sprite("Frames/Toggle_Knob"), ThemeColor.White, false);
            knob.rectTransform.sizeDelta = new Vector2(26, 26);
            knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = knob.rectTransform.pivot = new Vector2(1, 0.5f);

            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = AddButton(root.gameObject, hit, root);
            button.transition = Selectable.Transition.None;
            var view = root.gameObject.AddComponent<ToggleSwitchView>();
            SetRefs(view, ("track", track.GetComponent<ThemedGraphic>()), ("knob", knob.rectTransform));
            return root.gameObject;
        }

        private static GameObject BuildSlider()
        {
            var root = NewUI("Slider", null, new Vector2(130, 28));
            Fixed(root, 130, 28);
            var background = AddImage(root, "Track", KitAssets.Sprite("Frames/Slider_Track"), ThemeColor.SliderTrack, true);
            Band(background.rectTransform, 14);

            // Slider drives fill/handle anchors along x and stretches them on y, so the areas set the height
            // and the fill reaches back under the handle's half-width to the start of the track.
            var fillArea = NewUI("Fill Area", root);
            Band(fillArea, 14);
            fillArea.offsetMin = new Vector2(12, fillArea.offsetMin.y);
            fillArea.offsetMax = new Vector2(-12, fillArea.offsetMax.y);
            var fill = AddImage(fillArea, "Fill", KitAssets.Sprite("Frames/Slider_Track"), ThemeColor.Green, true);
            Stretch(fill.rectTransform, -12, 0, 0, 0);

            var handleArea = NewUI("Handle Slide Area", root);
            Stretch(handleArea, 12, 0, 12, 0);
            var handle = AddImage(handleArea, "Handle", KitAssets.Sprite("Frames/Slider_Knob"), ThemeColor.Yellow, false);
            handle.raycastTarget = true;
            handle.preserveAspect = true;
            handle.rectTransform.sizeDelta = new Vector2(26, 0);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.value = 0.6f;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            root.gameObject.AddComponent<SliderView>();
            return root.gameObject;
        }

        private static GameObject BuildListRow()
        {
            var root = NewUI("ListRow", null, new Vector2(342, 58));
            var frame = AddFrame(root, "Card", ThemeColor.White, ThemeColor.YellowShadow, Overhang);
            var row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 14, 0, 0);
            row.spacing = 12;
            row.childAlignment = TextAnchor.MiddleLeft;
            SetControl(row);

            var chip = AddChip(root, 40, ThemeColor.Green, KitAssets.Icon("howtoplay"), 20, out var icon);
            var label = AddText(root, "Label", "REPLAY TUTORIAL", ThemeFont.Heading, 21, ThemeColor.Ink, 3f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var accessory = NewUI("Accessory", root);
            var accessoryLayout = accessory.gameObject.AddComponent<HorizontalLayoutGroup>();
            accessoryLayout.childAlignment = TextAnchor.MiddleRight;
            SetControl(accessoryLayout);
            var chevron = AddIcon(accessory, "Chevron", KitAssets.Icon("next"), 22, ThemeColor.Ink);
            Fixed(chevron.rectTransform, 22, 22);

            var button = AddButton(root.gameObject, frame.Fill.Graphic, root);
            var view = root.gameObject.AddComponent<ListRowView>();
            SetRefs(view,
                ("button", button),
                ("chip", chip),
                ("icon", icon),
                ("label", label),
                ("labelColor", label.GetComponent<ThemedGraphic>()),
                ("accessory", accessory),
                ("chevron", chevron.GetComponent<ThemedGraphic>()));
            return root.gameObject;
        }

        private static GameObject BuildModalCard()
        {
            var root = NewUI("ModalCard", null, new Vector2(390, 844));
            Stretch(root, 0, 0, 0, 0);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1;
            var scrim = AddImage(root, "Scrim", null, ThemeColor.Scrim, false);
            scrim.raycastTarget = true;
            Stretch(scrim.rectTransform, 0, 0, 0, 0);

            var card = NewUI("Card", root, new Vector2(310, 300));
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            AddFrame(card, "ModalCard", ThemeColor.White, ThemeColor.InkShadow, Overhang);
            var column = card.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(14, 14, 22, 22);
            column.spacing = 14;
            column.childAlignment = TextAnchor.UpperCenter;
            SetControl(column, expandWidth: true);
            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = AddText(card, "Title", "PAUSED", ThemeFont.Heading, 48, ThemeColor.Red, 2f);
            var message = AddText(card, "Message", "Level 7 · 12 of 20 perfect", ThemeFont.Body, 13, ThemeColor.Muted, 0f);
            message.textWrappingMode = TextWrappingModes.Normal;
            var content = NewUI("Content", card);
            var contentColumn = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentColumn.spacing = 12;
            contentColumn.childAlignment = TextAnchor.UpperCenter;
            SetControl(contentColumn, expandWidth: true);

            var view = root.gameObject.AddComponent<ModalCardView>();
            SetRefs(view, ("card", card), ("title", title), ("message", message), ("content", content));
            return root.gameObject;
        }

        private static GameObject BuildToast()
        {
            var root = NewUI("Toast", null, new Vector2(390, 120));
            root.anchorMin = new Vector2(0, 0);
            root.anchorMax = new Vector2(1, 0);
            root.pivot = new Vector2(0.5f, 0);
            root.anchoredPosition = new Vector2(0, 110);
            root.sizeDelta = new Vector2(0, 60);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            group.interactable = false;

            var body = NewUI("Body", root, new Vector2(200, 48));
            body.anchorMin = body.anchorMax = body.pivot = new Vector2(0.5f, 0.5f);
            AddFrame(body, "Pill", ThemeColor.White, ThemeColor.InkShadow, Overhang);
            var row = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(22, 22, 10, 12);
            row.childAlignment = TextAnchor.MiddleCenter;
            SetControl(row);
            var fitter = body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var message = AddText(body, "Message", "All levels unlocked!", ThemeFont.BodyBold, 17, ThemeColor.Ink, 0f);

            var view = root.gameObject.AddComponent<ToastView>();
            SetRefs(view, ("body", body), ("message", message));
            return root.gameObject;
        }

        private static GameObject BuildScreenBackground()
        {
            var root = NewUI("ScreenBackground", null, new Vector2(390, 844));
            Stretch(root, 0, 0, 0, 0);
            var fill = AddImage(root, "Fill", null, ThemeColor.Yellow, false);
            Stretch(fill.rectTransform, 0, 0, 0, 0);
            var doodle = AddImage(root, "Doodle", KitAssets.Sprite("Patterns/DoodlePattern"), ThemeColor.Doodle, false);
            Stretch(doodle.rectTransform, 0, 0, 0, 0);
            doodle.preserveAspect = true;
            var fitter = doodle.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 428.9f / 853.6f;
            return root.gameObject;
        }

        // ---------- Helpers ----------

        private static GameObject Save(GameObject root, string name)
        {
            var path = $"{PrefabDir}/{name}.prefab";
            root.name = name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static RectTransform NewUI(string name, Transform parent, Vector2? size = null)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)go.transform;
            if (parent != null)
                rect.SetParent(parent, false);
            if (size.HasValue)
                rect.sizeDelta = size.Value;
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

        private static void Band(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0, height);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Fixed(RectTransform rect, float width, float height)
        {
            var element = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            element.minHeight = element.preferredHeight = height;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetControl(HorizontalOrVerticalLayoutGroup layout, bool expandWidth = false)
        {
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = false;
        }

        private static Image AddImage(RectTransform parent, string name, Sprite sprite, ThemeColor token, bool sliced)
        {
            var rect = NewUI(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (sliced)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }

            var themed = rect.gameObject.AddComponent<ThemedGraphic>();
            themed.Theme = _theme;
            themed.Token = token;
            return image;
        }

        private static FrameView AddFrame(RectTransform parent, string frame, ThemeColor fill, ThemeColor? shadowColor,
            Vector4 overhang, bool shadow = true)
        {
            var sliced = frame is not ("Circle" or "Chip" or "StatCircle");
            // Overhanging() moves each layer to the back, so add the fill first and the shadow last.
            var f = AddImage(parent, "FrameFill", KitAssets.Sprite($"Frames/Frame_{frame}"), fill, sliced);
            Overhanging(f.rectTransform, overhang);

            ThemedGraphic shadowGraphic = null;
            if (shadow)
            {
                var s = AddImage(parent, "FrameShadow", KitAssets.Sprite($"Frames/Frame_{frame}_Shadow"),
                    shadowColor ?? ThemeColor.YellowShadow, sliced);
                Overhanging(s.rectTransform, overhang);
                shadowGraphic = s.GetComponent<ThemedGraphic>();
            }

            var view = parent.gameObject.AddComponent<FrameView>();
            SetRefs(view, ("fill", f.GetComponent<ThemedGraphic>()), ("shadow", shadowGraphic));
            return view;
        }

        private static void Overhanging(RectTransform rect, Vector4 overhang)
        {
            Stretch(rect, -overhang.x, -overhang.y, -overhang.z, -overhang.w);
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rect.SetAsFirstSibling();
        }

        private static ThemedGraphic AddChip(RectTransform parent, float size, ThemeColor color, Sprite iconSprite,
            float iconSize, out Image icon)
        {
            var chipRect = NewUI("Chip", parent, new Vector2(size, size));
            Fixed(chipRect, size, size);
            var chip = AddImage(chipRect, "ChipFill", KitAssets.Sprite("Frames/Frame_Chip"), color, false);
            Overhanging(chip.rectTransform, ChipOverhang * (size / 40f));
            icon = AddIcon(chipRect, "Icon", iconSprite, iconSize, ThemeColor.White);
            return chip.GetComponent<ThemedGraphic>();
        }

        private static Image AddIcon(RectTransform parent, string name, Sprite sprite, float size, ThemeColor token)
        {
            var icon = AddImage(parent, name, sprite, token, false);
            icon.preserveAspect = true;
            icon.rectTransform.sizeDelta = new Vector2(size, size);
            return icon;
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string name, string text, ThemeFont font, float size,
            ThemeColor color, float spacingPercent)
        {
            var rect = NewUI(name, parent);
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
            var themed = rect.gameObject.AddComponent<ThemedGraphic>();
            themed.Theme = _theme;
            themed.Token = color;
            return tmp;
        }

        private static Button AddButton(GameObject go, Graphic target, RectTransform pressTarget)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = target;
            button.transition = Selectable.Transition.None;
            target.raycastTarget = true;
            var press = go.AddComponent<PressScale>();
            SetRefs(press, ("target", pressTarget));
            return button;
        }

        private static void SetRefs(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
                so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            switch (value)
            {
                case int i when prop.propertyType == SerializedPropertyType.Enum:
                    prop.enumValueIndex = i;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
