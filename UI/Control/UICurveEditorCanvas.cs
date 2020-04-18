using Leap;
using Leap.Unity.Swizzle;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class UICurveEditorCanvas : MaskableGraphic, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private readonly List<CurveLine> _lines = new List<CurveLine>();
        private readonly Dictionary<CurveLine, float> _scrubberPositions = new Dictionary<CurveLine, float>();

        private CurveEditorPoint _selectedPoint;

        private Vector2 _cameraPosition;
        private Vector2 _dragStartPosition;
        private Vector2 _dragTranslation;

        private bool _showScrubbers;

        private float zoomValue = 100;
        private Matrix4x4 _viewMatrix => Matrix4x4.TRS(_cameraPosition + _dragTranslation, Quaternion.identity, new Vector3(zoomValue, zoomValue, 1));
        private Matrix4x4 _viewMatrixInv => _viewMatrix.inverse;

        public CurveEditorPoint selectedPoint
        {
            get { return _selectedPoint; }
            set { SetSelectedPoint(value); }
        }

        public bool showScrubbers
        {
            get { return _showScrubbers; }
            set { _showScrubbers = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var min = _viewMatrixInv.MultiplyPoint3x4(-rectTransform.sizeDelta).xy();
            var max = _viewMatrixInv.MultiplyPoint3x4(rectTransform.sizeDelta).xy();
            var viewBounds = new Bounds((min + max) / 2, (max - min) / 2);

            foreach (var line in _lines)
                line.PopulateMesh(vh, _viewMatrix, viewBounds);

            if (_showScrubbers)
            {
                //TODO: colors
                foreach (var kv in _scrubberPositions)
                    vh.DrawLine(new Vector2(kv.Value, min.y), new Vector2(kv.Value, max.y), 0.02f, Color.black, _viewMatrix);

                foreach (var kv in _scrubberPositions)
                {
                    var line = kv.Key;
                    var time = kv.Value;

                    var position = new Vector2(time, line.curve.Evaluate(time));
                    vh.DrawCircle(position, 0.05f, Color.white, _viewMatrix);
                }
            }
        }

        internal CurveLine CreateCurve(IStorableAnimationCurve storable, UICurveLineColors colors, float thickness)
        {
            var line = new CurveLine(storable, colors);
            _lines.Add(line);
            _scrubberPositions.Add(line, line.curve.keys.First().time);
            SetVerticesDirty();
            return line;
        }

        internal void RemoveCurve(CurveLine line)
        {
            _lines.Remove(line);
            _scrubberPositions.Remove(line);
            SetVerticesDirty();
        }

        public void SetSelectedPoint(CurveEditorPoint point)
        {
            foreach (var line in _lines)
                line.SetSelectedPoint(null);
            
            if (point != null)
            {
                _lines.Remove(point.owner);
                _lines.Add(point.owner);

                point.owner.SetSelectedPoint(point);
            }

            _selectedPoint = point;
            SetVerticesDirty();
        }

        public void SetScrubberPosition(CurveLine line, float time)
        {
            if (!_scrubberPositions.ContainsKey(line))
                return;

            _scrubberPositions[line] = time;
            SetVerticesDirty();
        }

        protected void Update()
        {
            if (_selectedPoint != null)
            {
                if (Input.GetKeyDown(KeyCode.Delete))
                {
                    _selectedPoint.owner.DestroyPoint(_selectedPoint);
                    _selectedPoint.owner.SetCurveFromPoints();
                    SetSelectedPoint(null);
                    SetVerticesDirty();
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                zoomValue *= 1.1f;
                SetVerticesDirty();
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                zoomValue /= 1.1f;
                SetVerticesDirty();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (_selectedPoint?.OnBeginDrag(position) == true)
            {
                _selectedPoint.owner.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            var closest = _lines.SelectMany(l => l.points).OrderBy(p => Vector2.Distance(p.position, position)).FirstOrDefault();
            if (closest?.OnBeginDrag(position) == true)
            {
                SetSelectedPoint(closest);
                _selectedPoint.owner.SetCurveFromPoints();
                return;
            }

            _dragStartPosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (_selectedPoint?.OnDrag(position) == true)
            {
                _selectedPoint.owner.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            _dragTranslation = eventData.position - _dragStartPosition;
            SetVerticesDirty();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (_selectedPoint?.OnEndDrag(position) == true)
            {
                _selectedPoint.owner.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            _cameraPosition += _dragTranslation;
            _dragTranslation = Vector2.zero;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;

            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (_selectedPoint?.OnPointerClick(position) == true)
            {
                SetVerticesDirty();
                return;
            }

            var closest = _lines.SelectMany(l => l.points).OrderBy(p => Vector2.Distance(p.position, position)).FirstOrDefault();
            if (closest?.OnPointerClick(position) == true)
            {
                SetSelectedPoint(closest);
                return;
            }

            if (eventData.clickCount > 0 && eventData.clickCount % 2 == 0)
            {
                var closestLine = _lines.OrderBy(l => Mathf.Abs(position.y - l.curve.Evaluate(position.x))).FirstOrDefault();

                SetSelectedPoint(null);
                closestLine.CreatePoint(position);
                closestLine.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            SetSelectedPoint(null);
        }

        private bool ScreenToCanvasPosition(PointerEventData eventData, out Vector2 position)
        {
            position = Vector2.zero;

            Vector2 localPosition;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPosition))
                return false;

            position = _viewMatrixInv.MultiplyPoint3x4(localPosition);
            return true;
        }
    }
}
