using System;

namespace Game
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