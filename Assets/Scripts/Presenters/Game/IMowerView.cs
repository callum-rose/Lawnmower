using Game.Core;
using Lawnmower.Model;

namespace Lawnmower.Presenters
{
	public interface IMowerView
	{
		/// <summary>Creates the mower and places it on <paramref name="start"/> without animating a move.</summary>
		void Show(GridVector start);

		/// <summary>Animates a move (or a redo) along <see cref="MoveResult.Segments"/>: steps, then any jumps.</summary>
		void PlayMove(MoveResult move);

		/// <summary>Undoes a move: the mower goes back to <see cref="MoveResult.From"/> without animating.</summary>
		void RewindMove(MoveResult move);

		/// <summary>The mower tried to move into a tile it can't enter. Nothing changed.</summary>
		void PlayBump(MoveResult bump);

		void Hide();
	}
}
