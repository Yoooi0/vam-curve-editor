using CurveEditor.Utils;
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

        private Vector2 _cameraPosition = Vector2.zero;
        private Vector2 _dragStartPosition = Vector2.zero;
        private Vector2 _dragTranslation = Vector2.zero;
        private bool _showScrubbers = true;
        private bool _showGrid = true;
        private Matrix4x4 _viewMatrix = Matrix4x4.identity;

        private Matrix4x4 _viewMatrixInv => _viewMatrix.inverse;

        public CurveEditorPoint selectedPoint { get; private set; } = null;
        public bool allowViewDragging { get; set; } = true;
        public bool allowViewZooming { get; set; } = true;
        public bool allowKeyboardShortcuts { get; set; } = true;
        public bool readOnly { get; set; } = false;

        public bool showScrubbers
        {
            get { return _showScrubbers; }
            set { _showScrubbers = value; SetVerticesDirty(); }
        }

        public bool showGrid
        {
            get { return _showGrid; }
            set { _showGrid = value; SetVerticesDirty(); }
        }

        protected override void Awake()
        {
            base.Awake();
            UpdateViewMatrix();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var min = _viewMatrixInv.MultiplyPoint2d(Vector2.zero);
            var max = _viewMatrixInv.MultiplyPoint2d(rectTransform.sizeDelta);
            var viewBounds = new Bounds((min + max) / 2, max - min);

            if (_showGrid)
                PopulateGrid(vh, viewBounds);

            foreach (var line in _lines)
                line.PopulateMesh(vh, _viewMatrix, viewBounds);

            if (_showScrubbers)
                PopulateScrubbers(vh, viewBounds);
        }

        private void PopulateGrid(VertexHelper vh, Bounds bounds)
        {
            //TODO: colors, zoom
            var min = bounds.min;
            var max = bounds.max;
            for (var v = Mathf.Floor(min.x); v <= Mathf.Ceil(max.x); v += 0.5f)
                vh.AddLine(new Vector2(v, min.y), new Vector2(v, max.y), 0.01f, new Color(0.6f, 0.6f, 0.6f), _viewMatrix);
            for (var v = Mathf.Floor(min.y); v <= Mathf.Ceil(max.y); v += 0.5f)
                vh.AddLine(new Vector2(min.x, v), new Vector2(max.x, v), 0.01f, new Color(0.6f, 0.6f, 0.6f), _viewMatrix);

            if (min.y < 0 && max.y > 0)
                vh.AddLine(new Vector2(min.x, 0), new Vector2(max.x, 0), 0.04f, new Color(0.5f, 0.5f, 0.5f), _viewMatrix);
            if (min.x < 0 && max.x > 0)
                vh.AddLine(new Vector2(0, min.y), new Vector2(0, max.y), 0.04f, new Color(0.5f, 0.5f, 0.5f), _viewMatrix);
        }

        private void PopulateScrubbers(VertexHelper vh, Bounds viewBounds)
        {
            //TODO: colors
            foreach (var kv in _scrubberPositions)
            {
                // TODO: Refactor to calculate once per draw pass
                var scale = DrawScaleOffset.FromBounds(viewBounds, kv.Key.valueBounds);

                if (kv.Value < scale.valueBounds.min.x || kv.Value > scale.valueBounds.max.x)
                    continue;

                vh.AddLine(new Vector2(kv.Value, scale.valueBounds.min.y) * scale.ratio + scale.offset, new Vector2(kv.Value, scale.valueBounds.max.y) * scale.ratio + scale.offset, 0.02f, Color.black, _viewMatrix);
            }

            foreach (var kv in _scrubberPositions)
            {
                // TODO: Refactor to calculate once per draw pass
                var scale = DrawScaleOffset.FromBounds(viewBounds, kv.Key.valueBounds);

                if (kv.Value + 0.05f < scale.valueBounds.min.x || kv.Value - 0.05f > scale.valueBounds.max.x)
                    continue;

                vh.AddCircle(new Vector2(kv.Value, kv.Key.curve.Evaluate(kv.Value)) * scale.ratio + scale.offset, 0.05f, Color.white, _viewMatrix);
            }
        }

        protected void Update()
        {
            if (!allowKeyboardShortcuts)
                return;

            if (selectedPoint != null)
            {
                if (!readOnly && Input.GetKeyDown(KeyCode.Delete))
                {
                    selectedPoint.parent.DestroyPoint(selectedPoint);
                    selectedPoint.parent.SetCurveFromPoints();
                    SetSelectedPoint(null);
                    SetVerticesDirty();
                }
            }

            if (allowViewZooming)
            {
                // TODO: Do we want to use value bounds for this? Or use a value built from the viewBounds, valueBounds and scaleFactor?
                if (Input.GetKeyDown(KeyCode.W))
                {
                    foreach (var line in _lines)
                        line.valueBounds.Expand(2f);
                    SetVerticesDirty();
                }
                if (Input.GetKeyDown(KeyCode.S))
                {
                    foreach (var line in _lines)
                        line.valueBounds.Expand(0.5f);
                    SetVerticesDirty();
                }
            }
        }

        private void UpdateViewMatrix()
        {
            // TODO: readd zoom
            _viewMatrix = Matrix4x4.TRS(_cameraPosition + _dragTranslation, Quaternion.identity, new Vector3(100, 100, 1));
        }

        public void SetViewToFit()
        {
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (var point in _lines.SelectMany(l => l.points))
            {
                maxX = Mathf.Max(maxX, point.position.x);
                minX = Mathf.Min(minX, point.position.x);
                maxY = Mathf.Max(maxY, point.position.y);
                minY = Mathf.Min(minY, point.position.y);
            }
            var min = new Vector2(minX, minY);
            var max = new Vector2(maxX, maxY);
            var valueBounds = new Bounds((max - min) / 2, max - min);
            foreach (var line in _lines)
            {
                line.valueBounds = valueBounds;
            }
            SetVerticesDirty();
        }

        public CurveLine CreateCurve(IStorableAnimationCurve storable, UICurveLineColors colors, float thickness)
        {
            var line = new CurveLine(storable, colors);
            _lines.Add(line);
            _scrubberPositions.Add(line, line.curve.keys.First().time);
            SetVerticesDirty();
            return line;
        }

        public void RemoveCurve(CurveLine line)
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
                _lines.Remove(point.parent);
                _lines.Add(point.parent);

                point.parent.SetSelectedPoint(point);
            }

            selectedPoint = point;
            SetVerticesDirty();
        }

        public void SetScrubberPosition(CurveLine line, float time)
        {
            if (!_scrubberPositions.ContainsKey(line))
                return;

            _scrubberPositions[line] = time;
            SetVerticesDirty();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (selectedPoint?.OnBeginDrag(position) == true)
            {
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            var closest = _lines.SelectMany(l => l.points).OrderBy(p => Vector2.Distance(p.position, position)).FirstOrDefault();
            if (closest?.OnBeginDrag(position) == true)
            {
                SetSelectedPoint(closest);
                selectedPoint.parent.SetCurveFromPoints();
                return;
            }

            if (allowViewDragging)
            {
                _dragStartPosition = eventData.position;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (selectedPoint?.OnDrag(position) == true)
            {
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            if (allowViewDragging)
            {
                _dragTranslation = eventData.position - _dragStartPosition;
                UpdateViewMatrix();
                SetVerticesDirty();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (selectedPoint?.OnEndDrag(position) == true)
            {
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            if (allowViewDragging)
            {
                _cameraPosition += _dragTranslation;
                _dragTranslation = Vector2.zero;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;

            Vector2 position;
            if (!ScreenToCanvasPosition(eventData, out position))
                return;

            if (selectedPoint?.OnPointerClick(position) == true)
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
