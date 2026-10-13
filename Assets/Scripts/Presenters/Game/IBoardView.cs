using Lawnmower.Model;

namespace Lawnmower.Presenters
{
	/// <summary>The tile views. Each tile view observes its read-only model tile; it never changes it.</summary>
	public interface IBoardView
	{
		void Show(Level level);
		void Clear();
	}
}
