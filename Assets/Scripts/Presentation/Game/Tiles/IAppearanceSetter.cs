namespace Game.Tiles
{
    internal interface IAppearanceSetter
    {
        void SetAppearance(int grassHeight);

        void TransitionAppearance(int fromGrassHeight, int toGrassHeight);
    }

}
