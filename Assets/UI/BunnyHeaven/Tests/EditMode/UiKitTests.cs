using System;
using BunnyHeaven.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunnyHeaven.UI.Tests
{
    public class UiKitTests
    {
        private const string PrefabDir = "Assets/Prefabs/UI/BunnyHeaven";
        private const string ThemePath = "Assets/UI/BunnyHeaven/Theme/BunnyHeavenTheme.asset";

        private GameObject _instance;

        [TearDown]
        public void TearDown()
        {
            if (_instance != null)
                Object.DestroyImmediate(_instance);
        }

        private T Spawn<T>(string prefab) where T : Component
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{prefab}.prefab");
            Assert.IsNotNull(asset, $"{prefab} prefab is missing");
            _instance = Object.Instantiate(asset);
            var view = _instance.GetComponent<T>();
            Assert.IsNotNull(view, $"{prefab} has no {typeof(T).Name}");
            return view;
        }

        [Test]
        public void Theme_DefinesEveryTokenAndFont()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            Assert.IsNotNull(theme);
            foreach (ThemeColor token in Enum.GetValues(typeof(ThemeColor)))
                Assert.AreNotEqual(Color.magenta, theme.Get(token), token.ToString());
            foreach (ThemeFont font in Enum.GetValues(typeof(ThemeFont)))
                Assert.IsNotNull(theme.GetFont(font), font.ToString());
            Assert.IsNotNull(theme.headingShadowMaterial);
        }

        [Test]
        public void Theme_MatchesFigmaTokens()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(ThemePath);
            Assert.AreEqual("FFC94A", ColorUtility.ToHtmlStringRGB(theme.Get(ThemeColor.Yellow)));
            Assert.AreEqual("3B2A20", ColorUtility.ToHtmlStringRGB(theme.Get(ThemeColor.Ink)));
            Assert.AreEqual("5DBB4C", ColorUtility.ToHtmlStringRGB(theme.Get(ThemeColor.Green)));
            Assert.AreEqual("EE4D64", ColorUtility.ToHtmlStringRGB(theme.Get(ThemeColor.Red)));
        }

        [TestCase("Card", typeof(FrameView))]
        [TestCase("Pill", typeof(PillView))]
        [TestCase("RoundIconButton", typeof(ButtonView))]
        [TestCase("BigButton_Green", typeof(ButtonView))]
        [TestCase("BigButton_Yellow", typeof(ButtonView))]
        [TestCase("BigButton_Blue", typeof(ButtonView))]
        [TestCase("BigButton_Red", typeof(ButtonView))]
        [TestCase("ToggleSegment", typeof(ToggleSegmentView))]
        [TestCase("ToggleSwitch", typeof(ToggleSwitchView))]
        [TestCase("Slider", typeof(SliderView))]
        [TestCase("ListRow", typeof(ListRowView))]
        [TestCase("ModalCard", typeof(ModalCardView))]
        [TestCase("Toast", typeof(ToastView))]
        public void Prefab_HasViewAndNoMissingReferences(string prefab, Type viewType)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{prefab}.prefab");
            Assert.IsNotNull(asset);
            Assert.IsNotNull(asset.GetComponent(viewType));
            foreach (var component in asset.GetComponentsInChildren<Component>(true))
            {
                Assert.IsNotNull(component, $"{prefab} has a missing script");
                var so = new SerializedObject(component);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null)
                        Assert.AreEqual(default(EntityId), it.objectReferenceEntityIdValue,
                            $"{prefab}/{component.name}.{it.propertyPath} is a broken reference");
                }
            }

            foreach (var image in asset.GetComponentsInChildren<Image>(true))
                if (image.name.StartsWith("Frame") || image.name == "Icon")
                    Assert.IsNotNull(image.sprite, $"{prefab}/{image.name} has no sprite");
        }

        [TestCase("BigButton_Green", ThemeColor.Green, ThemeColor.White)]
        [TestCase("BigButton_Yellow", ThemeColor.Yellow, ThemeColor.Ink)]
        [TestCase("BigButton_Blue", ThemeColor.Blue, ThemeColor.White)]
        [TestCase("BigButton_Red", ThemeColor.Red, ThemeColor.White)]
        public void BigButtonVariants_UseTheirPalette(string prefab, ThemeColor fill, ThemeColor content)
        {
            var view = Spawn<ButtonView>(prefab);
            Assert.AreEqual(fill, view.transform.Find("FrameFill").GetComponent<ThemedGraphic>().Token);
            Assert.AreEqual(content, view.Label.GetComponent<ThemedGraphic>().Token);
        }

        [Test]
        public void ToggleSegment_SelectRaisesEventAndMovesHighlight()
        {
            var view = Spawn<ToggleSegmentView>("ToggleSegment");
            var raised = -1;
            view.SelectionChanged += i => raised = i;

            view.Select(0);

            Assert.AreEqual(0, raised);
            Assert.AreEqual(0, view.SelectedIndex);
            var highlights = view.GetComponentsInChildren<FrameView>(true);
            Assert.IsTrue(highlights[1].Fill.Graphic.enabled);
            Assert.IsFalse(highlights[2].Fill.Graphic.enabled);
        }

        [Test]
        public void ToggleSegment_SelectingCurrentDoesNotNotify()
        {
            var view = Spawn<ToggleSegmentView>("ToggleSegment");
            var count = 0;
            view.SelectionChanged += _ => count++;
            view.Select(view.SelectedIndex);
            view.SetSelectedWithoutNotify(0);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ToggleSwitch_ToggleFlipsStateAndTrackColour()
        {
            var view = Spawn<ToggleSwitchView>("ToggleSwitch");
            var track = view.transform.Find("Track").GetComponent<ThemedGraphic>();
            bool? raised = null;
            view.ValueChanged += v => raised = v;

            view.Toggle();

            Assert.IsFalse(view.IsOn);
            Assert.AreEqual(false, raised);
            Assert.AreEqual(ThemeColor.SliderTrack, track.Token);
        }

        [Test]
        public void ListRow_DangerColourTintsLabelAndChevron()
        {
            var view = Spawn<ListRowView>("ListRow");
            view.SetTextColor(ThemeColor.Red);
            Assert.AreEqual(ThemeColor.Red, view.Label.GetComponent<ThemedGraphic>().Token);
            Assert.AreEqual(ThemeColor.Red, view.Accessory.Find("Chevron").GetComponent<ThemedGraphic>().Token);
        }
    }
}
