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

	public interface ILevelCatalog
	{
		IReadOnlyList<LevelSummary> GetLevels();
	}

	public interface ILevelSelectView
	{
		void ShowLevels(IReadOnlyList<LevelSummary> levels);

		/// <summary>The level index the player tapped (locked or not).</summary>
		event Action<int> LevelChosen;
	}

	public interface ILevelSelectNavigator
	{
		void PlayLevel(int index);
	}

	public sealed class LevelSelectPresenter : IDisposable
	{
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
			_levels = _catalog.GetLevels();
			_view.LevelChosen += OnLevelChosen;
			_view.ShowLevels(_levels);
		}

		public void Dispose()
		{
			if (!_isStarted)
			{
				return;
			}

			_isStarted = false;
			_view.LevelChosen -= OnLevelChosen;
		}

		private void OnLevelChosen(int index)
		{
			foreach (LevelSummary level in _levels)
			{
				if (level.Index != index)
				{
					continue;
				}

				if (!level.IsLocked)
				{
					_navigator.PlayLevel(index);
				}

				return;
			}
		}
	}
}
