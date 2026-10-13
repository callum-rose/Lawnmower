using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BunnyHeaven.UI.Tests
{
    public class UiKitPlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        private T Build<T>(string prefab) where T : Component
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/UI/BunnyHeaven/{prefab}.prefab");
            _root = new GameObject("Canvas", typeof(Canvas));
            return Object.Instantiate(asset, _root.transform).GetComponent<T>();
#else
            Assert.Ignore("Needs the editor asset database");
            return null;
#endif
        }

        [UnityTest]
        public IEnumerator ButtonView_ClickRaisesClickedAndAnyClicked()
        {
            var view = Build<ButtonView>("BigButton_Green");
            yield return null;
            var clicked = 0;
            ButtonView any = null;
            void OnAny(ButtonView b) => any = b;
            view.Clicked += () => clicked++;
            ButtonView.AnyClicked += OnAny;
            try
            {
                view.Button.onClick.Invoke();
            }
            finally
            {
                ButtonView.AnyClicked -= OnAny;
            }

            Assert.AreEqual(1, clicked);
            Assert.AreSame(view, any);
        }

        [UnityTest]
        public IEnumerator PressScale_ShrinksWhilePressedAndRecovers()
        {
            var view = Build<ButtonView>("BigButton_Green");
            yield return null;
            var press = view.GetComponent<PressScale>();
            var data = new PointerEventData(EventSystem.current);

            press.OnPointerDown(data);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.Less(view.transform.localScale.x, 0.95f);

            press.OnPointerUp(data);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreEqual(1f, view.transform.localScale.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator ModalCard_ShowThenHideDeactivates()
        {
            var view = Build<ModalCardView>("ModalCard");
            view.gameObject.SetActive(false);
            var hidden = false;
            view.Hidden += () => hidden = true;

            view.Show();
            Assert.IsTrue(view.gameObject.activeSelf);
            yield return new WaitForSecondsRealtime(0.3f);
            view.Hide();
            yield return new WaitForSecondsRealtime(0.3f);

            Assert.IsTrue(hidden);
            Assert.IsFalse(view.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Toast_ShowsThenHidesItself()
        {
            var view = Build<ToastView>("Toast");
            yield return null;
            view.Show("hello", 0.1f);
            Assert.IsTrue(view.IsShowing);
            yield return new WaitForSecondsRealtime(0.8f);
            Assert.IsFalse(view.IsShowing);
            Assert.IsFalse(view.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Slider_ReportsValueChanges()
        {
            var view = Build<SliderView>("Slider");
            yield return null;
            var reported = -1f;
            view.ValueChanged += v => reported = v;
            view.Value = 0.25f;
            Assert.AreEqual(0.25f, reported, 0.001f);
            view.SetValueWithoutNotify(0.9f);
            Assert.AreEqual(0.25f, reported, 0.001f);
        }
    }
}
