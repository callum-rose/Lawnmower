using Game.Core;
using Game.Tiles;

namespace Game.Levels
{
	internal interface ILevelData : IReadOnlyLevelData
	{
		void Init(Tilee[,] tiles, GridVector mowerStartPosition);
		void SetTile(int x, int y, Tilee tile);
		void SetTile(GridVector position, Tilee tile);
	}
}