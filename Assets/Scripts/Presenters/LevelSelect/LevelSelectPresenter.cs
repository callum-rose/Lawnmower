using System;
using System.Collections.Generic;

namespace Lawnmower.Presenters
{
	public readonly struct LevelSummary
	{
		/// <summary>Position in the level list (0-based). The level's number is Index + 1.</summary>
		public int Index { get; }
		public Guid Id { get; }
		public bool IsLocked { get; }
		public bool IsCompleted { get; }

		/// <summary>Completed at or under par at least once.</summary>
		public bool IsPerfect { get; }

		/// <summary>Fewest moves it's been completed in; null if it hasn't been.</summary>
		public int? Best { get; }

		/// <summary>The first unlocked level not yet completed: the big "NEXT!" node.</summary>
		public bool IsNext { get; }

		public int Number => Index + 1;

		/// <summary>How the Level Select node draws this level.</summary>
		public LevelNodeState NodeState =>
			IsLocked ? LevelNodeState.Locked
			: IsNext ? LevelNodeState.Next
			: IsPerfect ? LevelNodeState.Perfect
			: IsCompleted ? LevelNodeState.Completed
			: LevelNodeState.Open;

		public LevelSummary(int index, Guid id, bool isLocked, bool isCompleted = false, bool isPerfect = false,
			int? best = null, bool isNext = false)
		{
			Index = index;
			Id = id;
			IsLocked = isLocked;
			IsCompleted = isCompleted;
			IsPerfect = isPerfect;
			Best = best;
			IsNext = isNext;
		}
	}

	/// <summary>The look of one node on the Level Select garden path.</summary>
	public enum LevelNodeState
	{
		/// <summary>Not playable yet: flat orange blob with a lock, no number.</summary>
		Locked,

		/// <summary>Unlocked, not completed and not the next level (only after unlock-all): white node, ink number.</summary>
		Open,

		/// <summary>The first unlocked level not yet completed: the big white node with "NEXT!" and the bunny.</summary>
		Next,

		/// <summary>Completed over par: green node, no badge.</summary>
		Completed,

		/// <summary>Completed at or under par: green node with the yellow tick badge.</summary>
		Perfect
	}

	/// <summary>A tab at the top of Level Select. Only the first world has levels; the others are "coming soon".</summary>
	public readonly struct WorldSummary
	{
		public string Name { get; }
		public bool IsLocked { get; }

		public WorldSummary(string name, bool isLocked)
		{
			Name = name;
			IsLocked = isLocked;
		}
	}

	public interface ILevelCatalog
	{
		IReadOnlyList<LevelSummary> GetLevels();

		/// <summary>Raised when any level's lock, completion or perfect state may have changed.</summary>
		event Action Changed;
	}

	public interface ILevelSelectView
	{
		void ShowWorlds(IReadOnlyList<WorldSummary> worlds, int selected);
		void ShowLevels(IReadOnlyList<LevelSummary> levels);

		/// <summary>The perfects pill: levels finished at or under par / levels in the list.</summary>
		void ShowPerfects(int perfects, int total);

		/// <summary>A locked node was tapped: nudge it and say why it's locked.</summary>
		void ShowLevelLocked(int index, string message);

		/// <summary>A locked world tab was tapped.</summary>
		void ShowWorldComingSoon(string message);

		/// <summary>The level index the player tapped (locked or not).</summary>
		event Action<int> LevelChosen;

		/// <summary>The world tab index the player tapped.</summary>
		event Action<int> WorldChosen;

		event Action BackRequested;
	}

	public interface ILevelSelectNavigator
	{
		void PlayLevel(int index);
		void GoToMainMenu();
	}

	public sealed class LevelSelectPresenter : IDisposable
	{
		/// <summary>The world tabs. Only Back Garden has levels yet (product decision 4).</summary>
		public static readonly IReadOnlyList<WorldSummary> Worlds = new[]
		{
			new WorldSummary("BACK GARDEN", false),
			new WorldSummary("THE PARK", true),
			new WorldSummary("SEASIDE", true)
		};

		private readonly ILevelSelectView _view;
		private readonly ILevelCatalog _catalog;
		private readonly ILevelSelectNavigator _navigator;

		private IReadOnlyList<LevelSummary> _levels = Array.Empty<LevelSummary>();
		private bool _isStarted;

		public LevelSelectPresenter(ILevelSelectView view, ILevelCatalog catalog, ILevelSelectNavigator navigator)
		{
			_view = view ?? throw new ArgumentNullException(nameof(view));
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
			_navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
		}

		public void Start()
		{
			if (_isStarted)
			{
				return;
			}

			_isStarted = true;
			_view.LevelChosen += OnLevelChosen;
			_view.WorldChosen += OnWorldChosen;
			_view.BackRequested += OnBackRequested;
			_catalog.Changed += Refresh;
			_view.ShowWorlds(Worlds, 0);
			Refresh();
		}

		public void Dispose()
		{
			if (!_isStarted)
			{
				return;
			}

			_isStarted = false;
			_view.LevelChosen -= OnLevelChosen;
			_view.WorldChosen -= OnWorldChosen;
			_view.BackRequested -= OnBackRequested;
			_catalog.Changed -= Refresh;
		}

		private void Refresh()
		{
			_levels = _catalog.GetLevels();

			int perfects = 0;
			foreach (LevelSummary level in _levels)
			{
				if (level.IsPerfect)
				{
					perfects++;
				}
			}

			_view.ShowLevels(_levels);
			_view.ShowPerfects(perfects, _levels.Count);
		}

		private void OnLevelChosen(int index)
		{
			foreach (LevelSummary level in _levels)
			{
				if (level.Index != index)
				{
					continue;
				}

				if (level.IsLocked)
				{
					_view.ShowLevelLocked(index, index > 0 ? $"Finish level {index} first!" : "Locked!");
				}
				else
				{
					_navigator.PlayLevel(index);
				}

				return;
			}
		}

		private void OnWorldChosen(int index)
		{
			if (index < 0 || index >= Worlds.Count || !Worlds[index].IsLocked)
			{
				return;
			}

			_view.ShowWorldComingSoon($"{ToTitle(Worlds[index].Name)}: coming soon!");
		}

		private void OnBackRequested() => _navigator.GoToMainMenu();

		private static string ToTitle(string upper)
		{
			char[] chars = upper.ToLowerInvariant().ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				if (i == 0 || chars[i - 1] == ' ')
				{
					chars[i] = char.ToUpperInvariant(chars[i]);
				}
			}

			return new string(chars);
		}
	}
}
