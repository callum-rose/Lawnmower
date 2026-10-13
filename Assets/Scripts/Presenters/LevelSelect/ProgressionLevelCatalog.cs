using System;
using System.Collections.Generic;

namespace Lawnmower.Presenters
{
	/// <summary>The level list as Level Select shows it: lock, completed, perfect, best and "NEXT!" from <see cref="Progression"/>.</summary>
	public sealed class ProgressionLevelCatalog : ILevelCatalog
	{
		private readonly Progression _progression;

		public ProgressionLevelCatalog(Progression progression)
		{
			_progression = progression ?? throw new ArgumentNullException(nameof(progression));
		}

		public IReadOnlyList<LevelSummary> GetLevels()
		{
			IReadOnlyList<Guid> levels = _progression.Levels;
			Guid? next = _progression.NextToPlay();
			List<LevelSummary> result = new List<LevelSummary>(levels.Count);

			for (int i = 0; i < levels.Count; i++)
			{
				Guid id = levels[i];
				result.Add(new LevelSummary(i, id, !_progression.IsUnlocked(id), _progression.IsCompleted(id),
					_progression.IsPerfect(id), _progression.Best(id), next == id));
			}

			return result;
		}
	}
}
