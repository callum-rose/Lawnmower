using System;
using System.Collections.Generic;

namespace Lawnmower.Presenters.Tests
{
	public sealed class FakeLevelSelectView : ILevelSelectView
	{
		public event Action<int> LevelChosen;
		public event Action<int> WorldChosen;
		public event Action BackRequested;

		public IReadOnlyList<LevelSummary> Shown;
		public IReadOnlyList<WorldSummary> Worlds;
		public int SelectedWorld = -1;
		public int ShowCount;
		public (int perfects, int total) Perfects;
		public readonly List<int> LockedShown = new List<int>();
		public readonly List<string> Messages = new List<string>();

		public void ShowWorlds(IReadOnlyList<WorldSummary> worlds, int selected)
		{
			Worlds = worlds;
			SelectedWorld = selected;
		}

		public void ShowLevels(IReadOnlyList<LevelSummary> levels)
		{
			Shown = levels;
			ShowCount++;
		}

		public void ShowPerfects(int perfects, int total) => Perfects = (perfects, total);

		public void ShowLevelLocked(int index, string message)
		{
			LockedShown.Add(index);
			Messages.Add(message);
		}

		public void ShowWorldComingSoon(string message) => Messages.Add(message);

		public void Tap(int index) => LevelChosen?.Invoke(index);
		public void TapWorld(int index) => WorldChosen?.Invoke(index);
		public void PressBack() => BackRequested?.Invoke();
		public bool HasListeners => LevelChosen != null || WorldChosen != null || BackRequested != null;
	}

	public sealed class FakeLevelSelectNavigator : ILevelSelectNavigator
	{
		public readonly List<int> Played = new List<int>();
		public int MainMenuCount;

		public void PlayLevel(int index) => Played.Add(index);
		public void GoToMainMenu() => MainMenuCount++;
	}
}
