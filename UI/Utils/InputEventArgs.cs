using System;
using UnityEngine;

namespace CurveEditor.UI
{
    public class InputEventArgs : EventArgs
    {
        public KeyCode Key { get; private set; }
        public bool Pressed { get; private set; }
        public bool Released => !Pressed;

        public InputEventArgs(KeyCode key, bool pressed)
        {
            Key = key;
            Pressed = pressed;
        }
    }
}
