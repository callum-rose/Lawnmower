using System;
using IUnified;

namespace Game.Levels
{
    internal interface IHasEditMode
    {
        bool IsEditMode { get; set; }
    }

    [Serializable]
    internal class IHasEditModeContainer : IUnifiedContainer<IHasEditMode>
    {

    }
}