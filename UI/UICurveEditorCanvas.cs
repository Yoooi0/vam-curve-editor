using CurveEditor.Utils;
using Leap.Unity.Swizzle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly Dictionary<IStorableAnimationCurve, CurveLine> _storableToLineMap = new Dictionary<IStorableAnimationCurve, CurveLine>();
        private UICurveEditorSettings _settings;

        private Vector2 _cameraPosition = Vector2.zero;
        private Vector2 _dragStartPosition = Vector2.zero;
        private Vector2 _dragTranslation = Vector2.zero;
        private Matrix4x4 _viewMatrix = Matrix4x4.identity;
        private float _zoom = 100;

        private bool _isCtrlDown, _isShiftDown, _isAltDown;

        public UICurveEditorSettings settings
        {
            get { return _settings; }
            set
            {
                if (_settings != null)
                    _settings.PropertyChanged -= OnSettingsChanged;

                _settings = value;
                _settings.PropertyChanged += OnSettingsChanged;
                OnSettingsChanged(this, new PropertyChangedEventArgs(null));
            }
        }

        public CurveEditorPoint selectedPoint { get; private set; } = null;

        public float zoom
        {
            get { return _zoom; }
            set
            {
                _zoom = value;
                UpdateViewMatrix();
                SetVerticesDirty();
            }
        }

        public Vector2 cameraPosition
        {
            get { return _cameraPosition; }
            set
            {
                _cameraPosition = value;
                UpdateViewMatrix();
                SetVerticesDirty();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            UpdateViewMatrix();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var viewBounds = GetViewBounds();
            if (settings.showGrid && _lines.Count > 0)
                PopulateGrid(vh, viewBounds, _lines.Last());

            if (settings.showScrubbers)
                foreach (var kv in _scrubberPositions)
                    kv.Key.PopulateScrubberLine(vh, _viewMatrix, viewBounds, kv.Value);

            foreach (var line in _lines)
                line.PopulateMesh(vh, _viewMatrix, viewBounds);

            if (settings.showScrubbers)
                foreach (var kv in _scrubberPositions)
                    kv.Key.PopulateScrubberPoints(vh, _viewMatrix, viewBounds, kv.Value);
        }

        private void PopulateGrid(VertexHelper vh, Rect viewBounds, CurveLine line)
        {
            if (line == null)
                return;

            var viewMin = viewBounds.min;
            var viewMax = viewBounds.max;
            var cellSize = line.GetGridCellSize(viewBounds, settings.gridCellCount);

            var offset = line.drawScale.Translate(Vector2.zero);
            var minX = Mathf.Floor((viewMin.x - offset.x) / cellSize.x) * cellSize.x;
            var maxX = Mathf.Ceil ((viewMax.x - offset.x) / cellSize.x) * cellSize.x;
            var minY = Mathf.Floor((viewMin.y - offset.y) / cellSize.y) * cellSize.y;
            var maxY = Mathf.Ceil ((viewMax.y - offset.y) / cellSize.y) * cellSize.y;

            for (var x = minX; x <= maxX; x += cellSize.x)
                vh.AddLine(new Vector2(x + offset.x, viewMin.y), new Vector2(x + offset.x, viewMax.y), settings.gridThickness, settings.gridColor, _viewMatrix);

            for (var y = minY; y <= maxY; y += cellSize.y)
                vh.AddLine(new Vector2(viewMin.x, y + offset.y), new Vector2(viewMax.x, y + offset.y), settings.gridThickness, settings.gridColor, _viewMatrix);

            if (viewMin.y - offset.y < 0 && viewMax.y - offset.y > 0)
                vh.AddLine(new Vector2(viewMin.x, offset.y), new Vector2(viewMax.x, offset.y), settings.gridAxisThickness, settings.gridAxisColor, _viewMatrix);
            if (viewMin.x - offset.x < 0 && viewMax.x - offset.x > 0)
                vh.AddLine(new Vector2(offset.x, viewMin.y), new Vector2(offset.x, viewMax.y), settings.gridAxisThickness, settings.gridAxisColor, _viewMatrix);
        }

        protected void Update()
        {
            _isCtrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            _isShiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _isAltDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr);

            if (!settings.allowKeyboardShortcuts)
                return;

            if (selectedPoint != null)
            {
                if (!settings.readOnly && Input.GetKeyDown(KeyCode.Delete))
                {
                    selectedPoint.parent.DestroyPoint(selectedPoint);
                    selectedPoint.parent.SetCurveFromPoints();
                    SetSelectedPoint(null);
                    SetVerticesDirty();
                }
            }

            if (settings.allowViewScaling)
            {
                if (Input.GetKeyDown(KeyCode.W))
                {
                    foreach (var line in _lines)
                    {
                        line.drawScale.ratio *= 2f;
                        line.SetPointsFromCurve();
                    }

                    SetVerticesDirty();
                }

                if (Input.GetKeyDown(KeyCode.S))
                {
                    foreach (var line in _lines)
                    {
                        line.drawScale.ratio *= 0.5f;
                        line.SetPointsFromCurve();
                    }

                    SetVerticesDirty();
                }
            }

            if (settings.allowViewZooming)
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    _zoom += 10f;
                    UpdateViewMatrix();
                    SetVerticesDirty();
                }

                if (Input.GetKeyDown(KeyCode.A))
                {
                    _zoom -= 10f;
                    UpdateViewMatrix();
                    SetVerticesDirty();
                }
            }
        }

        private void UpdateViewMatrix()
            => _viewMatrix = Matrix4x4.TRS(_cameraPosition + _dragTranslation, Quaternion.identity, new Vector3(_zoom, _zoom, 1));

        public void CreateCurve(IStorableAnimationCurve storable, CurveLineSettings settings = null)
        {
            var line = new CurveLine(storable, settings ?? new CurveLineSettings());
            _lines.Add(line);
            _scrubberPositions.Add(line, line.curve.keys.FirstOrDefault().time);
            _storableToLineMap.Add(storable, line);
            SetVerticesDirty();
        }

        public void RemoveCurve(IStorableAnimationCurve storable)
        {
            if (!_storableToLineMap.ContainsKey(storable))
                return;

            var line = _storableToLineMap[storable];
            _lines.Remove(line);
            _scrubberPositions.Remove(line);
            _storableToLineMap.Remove(storable);
            SetVerticesDirty();
        }

        public void UpdateCurve(IStorableAnimationCurve storable)
        {
            CurveLine line;
            if (!_storableToLineMap.TryGetValue(storable, out line))
                return;

            line.SetPointsFromCurve();
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (!ScreenPointToLocalPoint(eventData.pressPosition, eventData.pressEventCamera, out localPoint))
                return;

            var position = _viewMatrix.inverse.MultiplyPoint2d(localPoint);
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

            if (settings.allowViewDragging)
                _dragStartPosition = localPoint;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (!ScreenPointToLocalPoint(eventData.position, eventData.pressEventCamera, out localPoint))
                return;

            var position = _viewMatrix.inverse.MultiplyPoint2d(localPoint);
            var viewBounds = GetViewBounds();

            if (_isCtrlDown)
            {
                var line = selectedPoint.parent;
                var offset = line.drawScale.Translate(Vector2.zero);
                var gridSnap = line.GetGridCellSize(viewBounds, settings.gridCellCount);

                position.x = Mathf.Round((position.x - offset.x) / gridSnap.x) * gridSnap.x + offset.x;
                position.y = Mathf.Round((position.y - offset.y) / gridSnap.y) * gridSnap.y + offset.y;
            }

            if (!settings.allowViewDragging)
            {
                position.x = Mathf.Clamp(position.x, viewBounds.min.x, viewBounds.max.x);
                position.y = Mathf.Clamp(position.y, viewBounds.min.y, viewBounds.max.y);
            }

            if(selectedPoint != null)
            {
                if (selectedPoint.OnDrag(position))
                {
                    selectedPoint.parent.SetCurveFromPoints();
                    SetVerticesDirty();
                    return;
                }
            }

            if (settings.allowViewDragging)
            {
                _dragTranslation = localPoint - _dragStartPosition;
                UpdateViewMatrix();
                SetVerticesDirty();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (!ScreenPointToLocalPoint(eventData.position, eventData.pressEventCamera, out localPoint))
                return;

            var position = _viewMatrix.inverse.MultiplyPoint2d(localPoint);
            if (selectedPoint?.OnEndDrag(position) == true)
            {
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
                return;
            }

            if (settings.allowViewDragging)
            {
                _cameraPosition += _dragTranslation;
                _dragTranslation = Vector2.zero;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging)
                return;

            Vector2 localPoint;
            if (!ScreenPointToLocalPoint(eventData.position, eventData.pressEventCamera, out localPoint))
                return;

            var position = _viewMatrix.inverse.MultiplyPoint2d(localPoint);
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
                var closestLine = _lines.OrderBy(l => l.DistanceToPoint(position)).FirstOrDefault();

                var created = closestLine.CreatePoint(position);
                closestLine.SetCurveFromPoints();
                SetVerticesDirty();
                SetSelectedPoint(created);
                return;
            }

            SetSelectedPoint(null);
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO: not everything needs a redraw
            SetVerticesDirty();
        }

        public void SetViewToFit(Vector4 margin = new Vector4())
        {
            if (_lines.Count == 0)
                return;

            // Ensure view matrix is up to date
            UpdateViewMatrix();

            var valueMin = Vector2.positiveInfinity;
            var valueMax = Vector2.negativeInfinity;
            foreach (var line in _lines)
            {
                foreach (var key in line.curve.keys)
                {
                    var position = new Vector2(key.time, key.value);
                    valueMin = Vector2.Min(valueMin, position);
                    valueMax = Vector2.Max(valueMax, position);
                }
            }

            valueMin -= margin.xw();
            valueMax += margin.zy();

            var valueBounds = new Rect(valueMin, valueMax - valueMin);
            var drawScale = DrawScaleOffset.FromNormalizedValueBounds(valueBounds, GetViewBounds().size);
            foreach (var line in _lines)
                line.drawScale = new DrawScaleOffset(drawScale);

            _cameraPosition = -drawScale.Multiply(valueBounds.min) * _zoom;
            UpdateViewMatrix();
            SetVerticesDirty();
        }

        public void SetValueBounds(IStorableAnimationCurve storable, Rect valueBounds, bool normalizeToView = false, bool offsetToCenter = false)
        {
            // Ensure view matrix is up to date
            UpdateViewMatrix();

            CurveLine line;
            if (!_storableToLineMap.TryGetValue(storable, out line))
                return;

            var offset = offsetToCenter ? -valueBounds.min : valueBounds.min;
            if (normalizeToView)
                line.drawScale = DrawScaleOffset.FromNormalizedValueBounds(valueBounds, GetViewBounds().size, offset);
            else
                line.drawScale = DrawScaleOffset.FromValueBounds(valueBounds, offset);

            SetVerticesDirty();
        }
        public void SetValueBounds(IStorableAnimationCurve storable, Vector2 valueMin, Vector2 valueMax, bool normalizeToView = false, bool offsetToCenter = false)
            => SetValueBounds(storable, new Rect(valueMin, valueMax - valueMin), normalizeToView, offsetToCenter);

        public void SetScrubberPosition(float time)
        {
            foreach (var line in _lines)
                _scrubberPositions[line] = time;
            SetVerticesDirty();
        }

        public void SetScrubberPosition(IStorableAnimationCurve storable, float time)
        {
            CurveLine line;
            if (!_storableToLineMap.TryGetValue(storable, out line))
                return;

            _scrubberPositions[line] = time;
            SetVerticesDirty();
        }

        public void ToggleInHandleMode()
        {
            if (selectedPoint != null)
            {
                selectedPoint.inHandleMode = 1 - selectedPoint.inHandleMode;
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
            }
        }

        public void ToggleOutHandleMode()
        {
            if (selectedPoint != null)
            {
                selectedPoint.outHandleMode = 1 - selectedPoint.outHandleMode;
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
            }
        }

        public void ToggleHandleMode()
        {
            if (selectedPoint != null)
            {
                selectedPoint.handleMode = 1 - selectedPoint.handleMode;
                selectedPoint.parent.SetCurveFromPoints();
                SetVerticesDirty();
            }
        }

        public void SetLinear()
        {
            if (selectedPoint == null)
                return;

            var line = selectedPoint.parent;
            var idx = line.points.IndexOf(selectedPoint);
            var curve = line.curve;
            var key = curve[idx];

            if (idx > 0)
            {
                var prev = curve[idx - 1];
                prev.outTangent = key.inTangent = (key.value - prev.value) / (key.time - prev.time);
                curve.MoveKey(idx - 1, prev);
            }

            if (idx < curve.length - 1)
            {
                var next = curve[idx + 1];
                next.inTangent = key.outTangent = (next.value - key.value) / (next.time - key.time);
                curve.MoveKey(idx + 1, next);
            }

            curve.MoveKey(idx, key);
            line.SetPointsFromCurve();
            SetVerticesDirty();
        }

        private bool ScreenPointToLocalPoint(Vector2 screenPoint, Camera camera, out Vector2 position)
        {
            position = Vector2.zero;

            Vector2 localPosition;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, camera, out localPosition))
                return false;

            position = localPosition;
            return true;
        }

        private Rect GetViewBounds()
        {
            var viewMin = _viewMatrix.inverse.MultiplyPoint2d(Vector2.zero);
            var viewMax = _viewMatrix.inverse.MultiplyPoint2d(rectTransform.sizeDelta);
            return new Rect(viewMin, viewMax - viewMin);
        }
    }
}
