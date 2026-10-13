using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Tiles;
using Game.UndoSystem;

namespace Lawnmower.Model.Tests
{
	/// <summary>Builds simulations from <see cref="LevelText"/> and plays <see cref="MoveNotation"/> strings.</summary>
	public static class Sim
	{
		public static readonly GridVector U = GridVector.Up;
		public static readonly GridVector D = GridVector.Down;
		public static readonly GridVector L = GridVector.Left;
		public static readonly GridVector R = GridVector.Right;

		public static readonly GridVector[] Directions = { U, D, L, R };

		public static GridVector V(int x, int y) => new GridVector(x, y);

		public static LevelSimulation Create(string rows, int startX = 0, int startY = 0) =>
			Create(rows, startX, startY, out _);

		public static LevelSimulation Create(string rows, int startX, int startY, out UndoSystem undo)
		{
			undo = new UndoSystem();
			return Create(LevelText.Parse(rows, V(startX, startY)), undo);
		}

		public static LevelSimulation Create(LevelDefinition definition, IUndoSystem undo) =>
			new LevelSimulation(Level.FromDefinition(definition), undo);

		public static List<MoveResult> Play(LevelSimulation sim, string moves) =>
			MoveNotation.Parse(moves).Select(sim.Move).ToList();

		public static int Height(LevelSimulation sim, int x, int y) =>
			((GrassTile)sim.Level.GetTile(x, y)).GrassHeight.Value;

		public static TileSpec Spec(LevelSimulation sim, int x, int y) => sim.Level.GetTile(x, y).ToSpec();

		/// <summary>Undoes until the history is exhausted; returns how many steps were undone.</summary>
		public static int UndoAll(IUndoSystem undo)
		{
			int count = 0;
			while (undo.Undo())
			{
				count++;
			}

			return count;
		}
	}
}
