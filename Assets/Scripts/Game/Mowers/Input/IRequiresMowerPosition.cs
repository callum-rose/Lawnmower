using System;

namespace Game.Mowers.Input
{
    internal interface IRequiresMowerPosition
    {
        void Init(IMowerPosition mowerPosition);
    }

    [Serializable]
    internal class IRequiresMowerPositionContainer : IUnifiedContainer<IRequiresMowerPosition>
    {

    }
}