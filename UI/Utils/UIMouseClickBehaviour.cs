using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CurveEditor.UI
{
    public class UIMouseClickBehaviour : MonoBehaviour, IPointerClickHandler
    {
        public event EventHandler<PointerEventArgs> OnClick;

        public void OnPointerClick(PointerEventData eventData) => OnClick?.Invoke(this, new PointerEventArgs(eventData));
    }
}
