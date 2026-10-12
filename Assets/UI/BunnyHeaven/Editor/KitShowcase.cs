using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BunnyHeaven.UI.Editor
{
    /// <summary>
    /// Renders the kit prefabs into PNGs (in a throwaway scene) so they can be compared against Figma.
    /// Writes to Temp/BunnyHeavenKit/ unless given a folder.
    /// </summary>
    public static class KitShowcase
    {
        private const string Dir = KitPrefabBuilder.PrefabDir;

        [MenuItem("Bunny Heaven/UI Kit/Render Showcase")]
        public static void RenderMenu() => Render("Temp/BunnyHeavenKit");

        public static string Render(string folder)
        {
            Directory.CreateDirectory(folder);
            // A preview scene never touches the open scenes, so nothing is marked dirty.
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var menu = Path.Combine(folder, "kit_menu.png");
                RenderScreen(scene, menu, ThemeColor.Yellow, ThemeColor.Doodle, BuildMenuPage);
                var modal = Path.Combine(folder, "kit_modal.png");
                RenderScreen(scene, modal, ThemeColor.Teal, ThemeColor.White, BuildModalPage);
                return Path.GetFullPath(folder);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void RenderScreen(Scene scene, string path, ThemeColor background, ThemeColor doodle,
            System.Action<RectTransform> build)
        {
            var cameraGo = new GameObject("ShowcaseCamera");
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.AddComponent<Camera>();
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.cullingMask = 1 << 5;

            var canvasGo = new GameObject("ShowcaseCanvas", typeof(RectTransform)) { layer = 5 };
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = 0;

            const int width = 780, height = 1688;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.aspect = (float)width / height;

            var root = (RectTransform)canvasGo.transform;
            root.sizeDelta = new Vector2(390, 844);
            var bg = Spawn("ScreenBackground", root);
            bg.transform.Find("Fill").GetComponent<ThemedGraphic>().Token = background;
            bg.transform.Find("Doodle").GetComponent<ThemedGraphic>().Token = doodle;
            build(root);

            for (var i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var tmp in canvasGo.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    tmp.ForceMeshUpdate(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            }

            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(path, texture.EncodeToPNG());

            Object.DestroyImmediate(texture);
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(canvasGo);
            Object.DestroyImmediate(cameraGo);
        }

        private static GameObject Spawn(string prefab, RectTransform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/{prefab}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.gameObject.scene);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Place(GameObject go, float x, float y)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private static void BuildMenuPage(RectTransform root)
        {
            var pill = Spawn("Pill", root).GetComponent<PillView>();
            Place(pill.gameObject, 20, 20);
            pill.SetShadow(ThemeColor.YellowShadow);
            var progress = Spawn("Pill", root).GetComponent<PillView>();
            Place(progress.gameObject, 200, 20);
            progress.SetShadow(ThemeColor.YellowShadow);
            progress.SetChipColor(ThemeColor.Blue);
            progress.SetIcon(KitAssets.Icon("check"));
            progress.SetText("12/20");

            var x = 30f;
            foreach (var (color, icon, label) in new[]
                     {
                         (ButtonColor.Blue, "levels", "LEVELS"), (ButtonColor.Red, "settings", "SETTINGS"),
                         (ButtonColor.Green, "howtoplay", "HOW TO PLAY"), (ButtonColor.Yellow, "undo", "UNDO"),
                     })
            {
                var b = Spawn("RoundIconButton", root).GetComponent<ButtonView>();
                b.SetColor(color);
                b.SetIcon(KitAssets.Icon(icon));
                b.SetLabel(label);
                Place(b.gameObject, x, 90);
                x += 88;
            }

            var y = 200f;
            foreach (var (prefab, icon, label) in new[]
                     {
                         ("BigButton_Green", "play", "PLAY · LEVEL 7"), ("BigButton_Yellow", "undo", "UNDO LAST MOVE"),
                         ("BigButton_Blue", "restart", "RESTART"), ("BigButton_Red", "levels", "LEVELS"),
                     })
            {
                var b = Spawn(prefab, root).GetComponent<ButtonView>();
                b.SetIcon(KitAssets.Icon(icon));
                b.SetLabel(label);
                Place(b.gameObject, 45, y);
                y += 84;
            }

            var graphics = Spawn("ListRow", root).GetComponent<ListRowView>();
            Place(graphics.gameObject, 24, 540);
            graphics.SetLabel("GRAPHICS");
            graphics.SetIcon(KitAssets.Icon("sparkle"));
            graphics.SetChipColor(ThemeColor.Blue);
            graphics.SetShowChevron(false);
            Spawn("ToggleSegment", graphics.Accessory);

            var haptics = Spawn("ListRow", root).GetComponent<ListRowView>();
            Place(haptics.gameObject, 24, 607);
            haptics.SetLabel("HAPTICS");
            haptics.SetIcon(KitAssets.Icon("vibrate"));
            haptics.SetShowChevron(false);
            Spawn("ToggleSwitch", haptics.Accessory);

            var music = Spawn("ListRow", root).GetComponent<ListRowView>();
            Place(music.gameObject, 24, 674);
            music.SetLabel("MUSIC");
            music.SetIcon(KitAssets.Icon("music"));
            music.SetChipColor(ThemeColor.Blue);
            music.SetShowChevron(false);
            Spawn("Slider", music.Accessory);

            var reset = Spawn("ListRow", root).GetComponent<ListRowView>();
            Place(reset.gameObject, 24, 741);
            reset.SetLabel("RESET PROGRESS");
            reset.SetIcon(KitAssets.Icon("trash"));
            reset.SetChipColor(ThemeColor.Red);
            reset.SetTextColor(ThemeColor.Red);

            var toast = Spawn("Toast", root).GetComponent<ToastView>();
            toast.GetComponent<CanvasGroup>().alpha = 1;
            ((RectTransform)toast.transform).anchoredPosition = new Vector2(0, 0);
        }

        private static void BuildModalPage(RectTransform root)
        {
            var pause = Spawn("RoundIconButton", root).GetComponent<ButtonView>();
            pause.SetColor(ButtonColor.WhiteOnTeal);
            pause.SetIcon(KitAssets.Icon("pause"));
            pause.SetLabel("");
            Place(pause.gameObject, 18, 54);

            var modal = Spawn("ModalCard", root).GetComponent<ModalCardView>();
            modal.SetTitle("SCALPED!");
            modal.SetMessage("You mowed a finished patch down to the dirt. Undo and try another route.");
            var undo = Spawn("BigButton_Yellow", modal.Content).GetComponent<ButtonView>();
            undo.SetIcon(KitAssets.Icon("undo"));
            undo.SetLabel("UNDO LAST MOVE");
            // Instantiate under a preview-scene parent rather than new GameObject(), which would land in
            // (and dirty) the open scene.
            var row = Object.Instantiate(modal.Content.gameObject, modal.Content);
            row.name = "Row";
            foreach (Transform child in row.transform)
                Object.DestroyImmediate(child.gameObject);
            Object.DestroyImmediate(row.GetComponent<VerticalLayoutGroup>());
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            foreach (var (prefab, icon, label) in new[] { ("BigButton_Blue", "restart", "RESTART"), ("BigButton_Red", "levels", "LEVELS") })
            {
                var b = Spawn(prefab, (RectTransform)row.transform).GetComponent<ButtonView>();
                b.SetIcon(KitAssets.Icon(icon));
                b.SetLabel(label);
                b.Label.fontSize = 20;
                var element = b.gameObject.AddComponent<LayoutElement>();
                element.preferredHeight = 50;
                element.flexibleWidth = 1;
            }

            // Heading with the ink-shadow material, as used for screen titles.
            var title = Object.Instantiate(modal.Title.gameObject, root);
            var titleRect = Place(title, 0, 140);
            titleRect.sizeDelta = new Vector2(390, 110);
            var tmp = title.GetComponent<TMPro.TMP_Text>();
            tmp.text = "BUNNY\nHEAVEN";
            tmp.fontSize = 72;
            tmp.lineSpacing = -30;
            title.GetComponent<ThemedGraphic>().Token = ThemeColor.White;
            title.GetComponent<ThemedText>().InkShadow = true;

            foreach (var b in modal.Content.GetComponentsInChildren<ButtonView>())
                if (b.GetComponent<LayoutElement>() == null)
                    b.gameObject.AddComponent<LayoutElement>().preferredHeight = 66;
        }
    }
}
