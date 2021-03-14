using Game.Core;
using Game.UndoSystem;

namespace Game.Tiles
{
	public delegate void TraverseEvent(GridVector direction, Xor isInverted);
}