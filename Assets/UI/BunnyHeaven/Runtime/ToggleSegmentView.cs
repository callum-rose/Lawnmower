using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BunnyHeaven.UI
{
    /// <summary>Segmented choice (e.g. LOW/HIGH, SWIPE/ARROWS). The selected segment gets the green blob.</summary>
    public class ToggleSegmentView : MonoBehaviour
    {
        [Serializable]
        public class Segment
        {
            public Button button;
            public Image highlight;
            public TMP_Text label;
        }

        [SerializeField] private Segment[] segments = Array.Empty<Segment>();
        [SerializeField] private int selectedIndex;
        [SerializeField] private UiTheme theme;
        [SerializeField] private ThemeColor selectedText = ThemeColor.White;
        [SerializeField] private ThemeColor unselectedText = ThemeColor.Brown;

        public event Action<int> SelectionChanged;

        public int SelectedIndex => selectedIndex;
        public int Count => segments.Length;

        private void Awake()
        {
            for (var i = 0; i < segments.Length; i++)
            {
                var index = i;
                segments[i].button.onClick.AddListener(() => Select(index));
            }
        }

        private void OnEnable() => Refresh();

        public void SetLabels(params string[] labels)
        {
            for (var i = 0; i < segments.Length && i < labels.Length; i++)
                segments[i].label.text = labels[i];
        }

        /// <summary>Selects a segment as if the player tapped it.</summary>
        public void Select(int index)
        {
            if (index == selectedIndex)
                return;

            SetSelectedWithoutNotify(index);
            SelectionChanged?.Invoke(selectedIndex);
        }

        public void SetSelectedWithoutNotify(int index)
        {
            selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, segments.Length - 1));
            Refresh();
        }

        private void Refresh()
        {
            for (var i = 0; i < segments.Length; i++)
            {
                var on = i == selectedIndex;
                if (segments[i].highlight != null)
                    segments[i].highlight.enabled = on;
                if (theme != null && segments[i].label != null)
                    segments[i].label.color = theme.Get(on ? selectedText : unselectedText);
            }
        }
    }
}
