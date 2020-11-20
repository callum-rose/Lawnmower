using System;
using UnityEngine;

namespace UI.Buttons
{
    public struct ButtonInfo
    {
        public ButtonInfo(string message = null, Sprite icon = null, Action action = null) : this()
        {
            Message = message;
            Icon = icon;
            Action = action;
        }

        public string Message { get; }
        public Sprite Icon { get; }
        public Action Action { get; }
    }
}
