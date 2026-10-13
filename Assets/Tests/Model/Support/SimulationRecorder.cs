using System.Collections.Generic;
using Game.Core;
using Game.Tiles;

namespace Lawnmower.Model.Tests
{
	/// <summary>
	/// Records every event a simulation and its tiles raise, as readable lines in the order they
	/// arrive. Inverted (undo) events end in " inv".
	/// </summary>
	public sealed class SimulationRecorder
	{
		public List<string> Log { get; } = new List<string>();

		public SimulationRecorder(LevelSimulation sim)
		{
			sim.MowerPosition.ValueChangedFromTo += (from, to, inv) => Add($"pos {P(from)}->{P(to)}", inv);
			sim.StatusChanged += (from, to, inv) => Add($"status {from}->{to}", inv);
			sim.Moved += (result, inv) => Add($"moved {Dir(result.Direction)} {P(result.From)}->{P(result.To)}", inv);
			sim.Bumped += result => Log.Add($"bumped {Dir(result.Direction)} at {P(result.BumpedAt.Value)}");

			foreach (GridVector p in sim.Level.Positions)
			{
				Tile tile = sim.Level.GetTile(p);
				GridVector at = p;
				tile.TraversedOnto += (dir, inv) => Add($"onto {P(at)} {Dir(dir)}", inv);
				tile.TraversedAway += (dir, inv) => Add($"away {P(at)} {Dir(dir)}", inv);
				tile.BumpedInto += (dir, inv) => Add($"bump {P(at)} {Dir(dir)}", inv);

				if (tile is GrassTile grass)
				{
					grass.GrassHeight.ValueChangedFromTo += (from, to, inv) => Add($"grass {P(at)} {from}->{to}", inv);
				}
			}
		}

		public void Clear() => Log.Clear();

		public int Count(string prefix) => Log.FindAll(l => l.StartsWith(prefix)).Count;

		public static string P(GridVector v) => $"{v.x},{v.y}";

		/// <summary>U/D/L/R for unit moves, otherwise the raw offset (spring jumps).</summary>
		public static string Dir(GridVector v) => v.IsCardinalUnit ? MoveNotation.ToChar(v).ToString() : $"({v.x},{v.y})";

		private void Add(string line, bool inverted) => Log.Add(inverted ? line + " inv" : line);
	}
}
