using System;

namespace Lawnmower.Presenters
{
	/// <summary>Saved progress. G1 replaces the Unity implementation with the full progression service.</summary>
	public interface ILevelProgress
	{
		void RecordCompleted(Guid levelId, AttemptStats stats, int par);
	}
}
