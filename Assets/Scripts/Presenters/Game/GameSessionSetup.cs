using System;
using Lawnmower.Model;

namespace Lawnmower.Presenters
{
	/// <summary>What one play session needs to know about its level.</summary>
	public sealed class GameSessionSetup
	{
		public LevelDefinition Definition { get; }

		/// <summary>The solver's shortest solution length; 0 when unknown.</summary>
		public int Par { get; }

		public GameSessionSetup(LevelDefinition definition, int par)
		{
			Definition = definition ?? throw new ArgumentNullException(nameof(definition));
			Par = Math.Max(0, par);
		}
	}
}
