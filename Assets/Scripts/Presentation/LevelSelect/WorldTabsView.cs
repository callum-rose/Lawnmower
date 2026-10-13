using System;
using System.Collections.Generic;
using BunnyHeaven.UI;
using Lawnmower.Presenters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelSelect
{
	/// <summary>The world tabs above the garden path. The selected tab gets the white blob; locked tabs show a lock.</summary>
	public class WorldTabsView : MonoBehaviour
	{
		[Serializable]
		public class Tab
		{
			public Button button;
			public Image highlight;
			public TMP_Text label;
			public Image lockIcon;
		}

		[SerializeField] private UiTheme theme;
		[SerializeField] private Tab[] tabs = Array.Empty<Tab>();

		public event Action<int> TabChosen;

		public int Count => tabs.Length;

		private void Awake()
		{
			for (int i = 0; i < tabs.Length; i++)
			{
				int index = i;
				tabs[i].button.onClick.AddListener(() => TabChosen?.Invoke(index));
			}
		}

		public void Show(IReadOnlyList<WorldSummary> worlds, int selected)
		{
			for (int i = 0; i < tabs.Length; i++)
			{
				bool exists = i < worlds.Count;
				tabs[i].button.gameObject.SetActive(exists);

				if (!exists)
				{
					continue;
				}

				bool isSelected = i == selected;
				tabs[i].label.text = worlds[i].Name;
				tabs[i].label.color = theme.Get(isSelected ? ThemeColor.Ink : ThemeColor.Brown);
				tabs[i].highlight.enabled = isSelected;
				tabs[i].lockIcon.gameObject.SetActive(worlds[i].IsLocked);
			}
		}

		/// <summary>For tests and the play-through: tap tab <paramref name="index"/>.</summary>
		public void Press(int index) => tabs[index].button.onClick.Invoke();
	}
}
