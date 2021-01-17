using System;
using Game.Core;
using IUnified;

namespace Game.Levels
{
	public interface ILevelTraversalChecker
	{
		void Init(IReadOnlyLevelData levelData);
		TileTraversalStatus CanTraverseTo(GridVector position);
	}

	[Serializable]
	public class ILevelTraversalCheckerContainer : IUnifiedContainer<ILevelTraversalChecker>
	{
		
	}
}