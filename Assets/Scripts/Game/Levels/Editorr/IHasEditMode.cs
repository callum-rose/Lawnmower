using System;
using IUnified;

namespace Game.Levels.Editorr
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