using System;

namespace Game.Tiles
{
    [Serializable]
    public class GrassTileSetupData : BaseTileSetupData
    {
        public int grassHeight = 1;

        public GrassTileSetupData(int grassHeight)
        {
            this.grassHeight = grassHeight;
        }
    }
}
