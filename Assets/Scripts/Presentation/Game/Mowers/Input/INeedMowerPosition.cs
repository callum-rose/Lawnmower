using System;
using IUnified;

namespace Game.Mowers.Input
{
    internal interface INeedMowerPosition
    {
        void Set(IMowerPosition mowerPosition);
        void Clear();
    }

    [Serializable]
    internal class INeedMowerPositionContainer : IUnifiedContainer<INeedMowerPosition>
    {

    }
}