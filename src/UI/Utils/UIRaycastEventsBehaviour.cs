using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CurveEditor.UI
{
    public class UIRaycastEventsBehaviour : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        public event EventHandler<PointerEventArgs> DefaultOnPointerClick;
        public event EventHandler<PointerEventArgs> DefaultOnBeginDrag;
        public event EventHandler<PointerEventArgs> DefaultOnDrag;
        public event EventHandler<PointerEventArgs> DefaultOnEndDrag;

        private List<UICurveEditorPoint> RaycastEvent(PointerEventData data)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, results);
            return results.OrderByDescending(r => r.depth).Select(r => r.gameObject.GetComponent<UICurveEditorPoint>()).Where(o => o != null).ToList();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            foreach (var o in RaycastEvent(eventData))
                if (o.OnPointerClick(eventData))
                    return;

            DefaultOnPointerClick?.Invoke(this, new PointerEventArgs(eventData));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            foreach (var o in RaycastEvent(eventData))
                if (o.OnBeginDrag(eventData))
                    return;

            DefaultOnBeginDrag?.Invoke(this, new PointerEventArgs(eventData));
        }

        public void OnDrag(PointerEventData eventData)
        {
            foreach (var o in RaycastEvent(eventData))
                if (o.OnDrag(eventData))
                    return;

            DefaultOnDrag?.Invoke(this, new PointerEventArgs(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            foreach (var o in RaycastEvent(eventData))
                if (o.OnEndDrag(eventData))
                    return;

            DefaultOnEndDrag?.Invoke(this, new PointerEventArgs(eventData));
        }
    }
}
