using System;
using UnityEngine;

namespace UI.Buttons
{
    public struct ButtonInfo
    {
        public ButtonInfo(string message = null, IconType icon = IconType.None, Action action = null) : this()
        {
            Message = message;
            Icon = icon;
            Action = action;
        }

        public string Message { get; }
        public IconType Icon { get; }
        public Action Action { get; }
    }
}
