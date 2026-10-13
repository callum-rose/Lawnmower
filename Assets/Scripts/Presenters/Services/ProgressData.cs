using System;
using System.Collections.Generic;

namespace Lawnmower.Presenters
{
	/// <summary>One level's saved result. Plain fields so a JSON serialiser can write it.</summary>
	[Serializable]
	public sealed class LevelRecord
	{
		/// <summary>The level GUID ("N" format).</summary>
		public string id;

		public bool completed;

		/// <summary>Fewest moves the level was completed in; 0 until it's completed.</summary>
		public int best;

		/// <summary>Completed at or under par at least once.</summary>
		public bool perfect;

		public LevelRecord Clone() => new LevelRecord { id = id, completed = completed, best = best, perfect = perfect };
	}

	/// <summary>Everything <see cref="Progression"/> saves.</summary>
	[Serializable]
	public sealed class ProgressData
	{
		public const int CurrentVersion = 1;

		public int version = CurrentVersion;
		public bool unlockAll;
		public List<LevelRecord> levels = new List<LevelRecord>();

		public ProgressData Clone()
		{
			ProgressData copy = new ProgressData { version = version, unlockAll = unlockAll };

			foreach (LevelRecord level in levels)
			{
				if (level != null)
				{
					copy.levels.Add(level.Clone());
				}
			}

			return copy;
		}
	}

	/// <summary>Where <see cref="Progression"/> keeps its data. The Unity implementation is JSON in PlayerPrefs.</summary>
	public interface IProgressStore
	{
		/// <summary>The saved data, or null when nothing has been saved.</summary>
		ProgressData Load();

		void Save(ProgressData data);
	}

	/// <summary>Keeps progress in memory only: for tests, and for play-throughs that mustn't touch the player's save.</summary>
	public sealed class InMemoryProgressStore : IProgressStore
	{
		private ProgressData _data;

		public int SaveCount { get; private set; }

		public InMemoryProgressStore(ProgressData initial = null)
		{
			_data = initial?.Clone();
		}

		public ProgressData Load() => _data?.Clone();

		public void Save(ProgressData data)
		{
			_data = data?.Clone();
			SaveCount++;
		}
	}
}
