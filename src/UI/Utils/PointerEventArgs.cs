using System;
using UnityEngine.EventSystems;

namespace CurveEditor.UI
{
    public class PointerEventArgs : EventArgs
    {
        public PointerEventData Data { get; }
        public PointerEventArgs(PointerEventData data) { Data = data; }
    }
}
