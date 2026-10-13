namespace Lawnmower.Presenters
{
	/// <summary>The current attempt only. Bumps don't count as moves; an undo takes one move off.</summary>
	public readonly struct AttemptStats
	{
		public int Moves { get; }
		public int Undos { get; }
		public double Duration { get; }

		public AttemptStats(int moves, int undos, double duration)
		{
			Moves = moves;
			Undos = undos;
			Duration = duration;
		}

		public override string ToString() => $"{Moves} moves, {Undos} undos, {Duration:0.0}s";
	}
}
