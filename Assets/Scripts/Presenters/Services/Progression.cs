using System;
using System.Collections.Generic;

namespace Lawnmower.Presenters
{
	/// <summary>What <see cref="Progression.MarkCompleted"/> changed, for the Level Complete card.</summary>
	public readonly struct CompletionResult
	{
		public int Moves { get; }

		/// <summary>The best before this completion; null the first time the level is completed.</summary>
		public int? PreviousBest { get; }

		/// <summary>The best including this completion.</summary>
		public int Best { get; }

		/// <summary>This attempt was at or under par.</summary>
		public bool IsPerfect { get; }

		/// <summary>The level hadn't been perfected before.</summary>
		public bool IsFirstPerfect { get; }

		/// <summary>The level that this completion unlocked, if it unlocked one.</summary>
		public Guid? UnlockedLevel { get; }

		public bool IsFirstCompletion => !PreviousBest.HasValue;

		/// <summary>Beat an earlier best: show "New best: <see cref="Best"/> moves". False on a first completion.</summary>
		public bool IsNewBest => PreviousBest.HasValue && Moves < PreviousBest.Value;

		public CompletionResult(int moves, int? previousBest, int best, bool isPerfect, bool isFirstPerfect, Guid? unlockedLevel)
		{
			Moves = moves;
			PreviousBest = previousBest;
			Best = best;
			IsPerfect = isPerfect;
			IsFirstPerfect = isFirstPerfect;
			UnlockedLevel = unlockedLevel;
		}
	}

	/// <summary>
	/// Saved progress through the level list, keyed by level GUID. Levels unlock in order: the first is always open
	/// and each later one opens when the one before it is completed (or everything opens after <see cref="UnlockAll"/>).
	/// Every change is saved straight away.
	/// </summary>
	public sealed class Progression
	{
		private readonly IProgressStore _store;
		private readonly List<Guid> _order = new List<Guid>();
		private readonly Dictionary<Guid, int> _indexById = new Dictionary<Guid, int>();
		private readonly Dictionary<Guid, LevelRecord> _records = new Dictionary<Guid, LevelRecord>();

		private bool _unlockAll;

		/// <summary>Raised after any change (completion, unlock-all, reset, new level order).</summary>
		public event Action Changed;

		/// <summary>The level list in play order.</summary>
		public IReadOnlyList<Guid> Levels => _order;

		public int LevelCount => _order.Count;

		public bool IsAllUnlocked => _unlockAll;

		/// <summary>Levels in the list completed at or under par (the Level Select "perfects" pill).</summary>
		public int PerfectCount => Count(r => r.perfect);

		/// <summary>Levels in the list completed at least once.</summary>
		public int CompletedCount => Count(r => r.completed);

		public Progression(IEnumerable<Guid> levelOrder, IProgressStore store)
		{
			_store = store ?? throw new ArgumentNullException(nameof(store));
			SetOrder(levelOrder);
			Load();
		}

		#region Queries

		public bool IsUnlocked(Guid level)
		{
			if (!_indexById.TryGetValue(level, out int index))
			{
				return false;
			}

			return _unlockAll || index == 0 || IsCompleted(level) || IsCompleted(_order[index - 1]);
		}

		public bool IsCompleted(Guid level) => _records.TryGetValue(level, out LevelRecord r) && r.completed;

		public bool IsPerfect(Guid level) => _records.TryGetValue(level, out LevelRecord r) && r.perfect;

		/// <summary>Fewest moves the level has been completed in, or null if it hasn't been.</summary>
		public int? Best(Guid level) => _records.TryGetValue(level, out LevelRecord r) && r.completed ? r.best : (int?)null;

		/// <summary>The level after <paramref name="level"/> in the list, or null for the last (or an unknown) level.</summary>
		public Guid? NextLevel(Guid level)
		{
			if (!_indexById.TryGetValue(level, out int index) || index + 1 >= _order.Count)
			{
				return null;
			}

			return _order[index + 1];
		}

		/// <summary>The first unlocked level not yet completed (Level Select's "NEXT!" node); null when all are done.</summary>
		public Guid? NextToPlay()
		{
			foreach (Guid level in _order)
			{
				if (!IsCompleted(level) && IsUnlocked(level))
				{
					return level;
				}
			}

			return null;
		}

		/// <summary>Position in the list, or -1.</summary>
		public int IndexOf(Guid level) => _indexById.TryGetValue(level, out int index) ? index : -1;

		#endregion

		#region Changes

		/// <summary>
		/// Records a completion: keeps the lowest move count, and the perfect flag once earned. Levels outside the list
		/// (old or test levels) are recorded too but never unlock anything.
		/// </summary>
		public CompletionResult MarkCompleted(Guid level, int moves, bool perfect)
		{
			if (moves < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(moves), moves, "Moves can't be negative");
			}

			Guid? next = NextLevel(level);
			bool nextWasLocked = next.HasValue && !IsUnlocked(next.Value);

			if (!_records.TryGetValue(level, out LevelRecord record))
			{
				record = new LevelRecord { id = level.ToString("N") };
				_records.Add(level, record);
			}

			int? previousBest = record.completed ? record.best : (int?)null;
			bool wasPerfect = record.perfect;

			record.best = previousBest.HasValue ? Math.Min(previousBest.Value, moves) : moves;
			record.completed = true;
			record.perfect |= perfect;

			Guid? unlocked = nextWasLocked && IsUnlocked(next.Value) ? next : null;

			SaveAndNotify();

			return new CompletionResult(moves, previousBest, record.best, perfect, perfect && !wasPerfect, unlocked);
		}

		/// <summary>Opens every level (the hidden Main Menu long-press). Completions are kept.</summary>
		public void UnlockAll()
		{
			if (_unlockAll)
			{
				return;
			}

			_unlockAll = true;
			SaveAndNotify();
		}

		/// <summary>Forgets every completion, best and perfect, and re-locks everything after the first level.</summary>
		public void ResetProgress()
		{
			_unlockAll = false;
			_records.Clear();
			SaveAndNotify();
		}

		/// <summary>Replaces the level list (the saved records are kept, keyed by GUID).</summary>
		public void SetLevelOrder(IEnumerable<Guid> levelOrder)
		{
			SetOrder(levelOrder);
			Changed?.Invoke();
		}

		#endregion

		#region Methods

		private void SetOrder(IEnumerable<Guid> levelOrder)
		{
			if (levelOrder == null)
			{
				throw new ArgumentNullException(nameof(levelOrder));
			}

			_order.Clear();
			_indexById.Clear();

			foreach (Guid level in levelOrder)
			{
				if (_indexById.ContainsKey(level))
				{
					throw new ArgumentException($"Level {level} is in the list twice", nameof(levelOrder));
				}

				_indexById.Add(level, _order.Count);
				_order.Add(level);
			}
		}

		private void Load()
		{
			ProgressData data = _store.Load();

			if (data == null)
			{
				return;
			}

			_unlockAll = data.unlockAll;

			foreach (LevelRecord record in data.levels ?? new List<LevelRecord>())
			{
				if (record == null || !Guid.TryParse(record.id, out Guid id) || _records.ContainsKey(id))
				{
					continue;
				}

				LevelRecord copy = record.Clone();
				copy.id = id.ToString("N");

				if (!copy.completed)
				{
					copy.best = 0;
					copy.perfect = false;
				}

				_records.Add(id, copy);
			}
		}

		private void SaveAndNotify()
		{
			ProgressData data = new ProgressData { unlockAll = _unlockAll };

			foreach (LevelRecord record in _records.Values)
			{
				data.levels.Add(record.Clone());
			}

			_store.Save(data);
			Changed?.Invoke();
		}

		private int Count(Func<LevelRecord, bool> predicate)
		{
			int count = 0;

			foreach (Guid level in _order)
			{
				if (_records.TryGetValue(level, out LevelRecord record) && predicate(record))
				{
					count++;
				}
			}

			return count;
		}

		#endregion
	}
}
