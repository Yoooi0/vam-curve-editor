using CurveEditor.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class UICurveEditor
    {
        public readonly UIDynamic container;
        public readonly GameObject gameObject;

        private readonly GameObject _linesContainer;
        private readonly GameObject _scrubbersContainer;
        private readonly UICurveEditorColors _colors;
        private readonly List<UICurveLine> _lines;

        private readonly Dictionary<IStorableAnimationCurve, UICurveLine> _storableToLineMap;
        private readonly Dictionary<UICurveLine, GameObject> _lineToContainerMap;
        private UICurveEditorPoint _selectedPoint;
        private bool _readOnly;
        private bool _showScrubbers;

        public bool readOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;

                SetSelectedPoint(null);
                var canvasGroup = _linesContainer.GetComponent<CanvasGroup>();
                canvasGroup.interactable = !value;
                canvasGroup.blocksRaycasts = !value;
            }
        }

        public bool showScrubbers
        {
            get { return _showScrubbers; }
            set
            {
                _showScrubbers = value;
                var canvasGroup = _scrubbersContainer.GetComponent<CanvasGroup>();
                canvasGroup.alpha = value ? 1 : 0;
            }
        }

        public UICurveEditor(UIDynamic container, float width, float height, List<UIDynamicButton> buttons = null, UICurveEditorColors colors = null, bool readOnly = false)
        {
            var buttonContainerHeight = (buttons == null || buttons.Count == 0) ? 0 : 25;

            this.container = container;

            _storableToLineMap = new Dictionary<IStorableAnimationCurve, UICurveLine>();
            _lineToContainerMap = new Dictionary<UICurveLine, GameObject>();
            _lines = new List<UICurveLine>();
            _colors = colors ?? new UICurveEditorColors();

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, buttonContainerHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            var input = gameObject.AddComponent<UIInputBehaviour>();
            input.OnInput += OnInput;

            var backgroundContent = new GameObject();
            backgroundContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);
            backgroundImage.color = _colors.backgroundColor;

            _linesContainer = new GameObject();
            _linesContainer.transform.SetParent(gameObject.transform, false);
            _linesContainer.AddComponent<CanvasGroup>();

            var raycastEvents = _linesContainer.AddComponent<UIRaycastEventsBehaviour>();
            raycastEvents.DefaultOnPointerClick += OnLinesContainerClick;
            raycastEvents.DefaultOnDrag += OnLinesContainerDrag;
            raycastEvents.DefaultOnEndDrag += OnLinesContainerEndDrag;

            this.readOnly = readOnly;

            var lineContainerRectTranform = _linesContainer.AddComponent<RectTransform>();
            lineContainerRectTranform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            lineContainerRectTranform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            if (buttons != null && buttons.Count != 0)
            {
                var buttonContainer = new GameObject();
                buttonContainer.transform.SetParent(container.transform, false);

                var rectTransform = buttonContainer.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(0, -(height - buttonContainerHeight) / 2);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonContainerHeight);

                var gridLayout = buttonContainer.AddComponent<GridLayoutGroup>();
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = buttons.Count;
                gridLayout.spacing = new Vector2();
                gridLayout.cellSize = new Vector2(width / buttons.Count, buttonContainerHeight);
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                foreach(var button in buttons)
                    button.gameObject.transform.SetParent(gridLayout.transform, false);
            }

            _scrubbersContainer = new GameObject();
            _scrubbersContainer.transform.SetParent(gameObject.transform, false);
            _scrubbersContainer.AddComponent<CanvasGroup>();
        }

        public UICurveLine AddCurve(IStorableAnimationCurve storable, UICurveLineColors colors = null, float thickness = 4)
        {
            var lineContainer = new GameObject();
            lineContainer.transform.SetParent(_linesContainer.transform, false);

            var rectTransform = _linesContainer.GetComponent<RectTransform>();
            var line = lineContainer.AddComponent<UILine>();
            line.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.sizeDelta.x);
            line.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y);
            line.lineThickness = thickness;

            var scrubberContainer = new GameObject();
            scrubberContainer.transform.SetParent(_scrubbersContainer.transform, false);
            var scrubber = scrubberContainer.AddComponent<UIScrubber>();
            scrubber.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1);
            scrubber.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y * 2);

            var curveLine = new UICurveLine(storable, line, scrubber, colors);
            _lines.Add(curveLine);
            _storableToLineMap.Add(storable, curveLine);
            _lineToContainerMap.Add(curveLine, lineContainer);

            BindPoints(curveLine);
            return curveLine;
        }

        public void RemoveCurve(IStorableAnimationCurve storable)
        {
            if (!_storableToLineMap.ContainsKey(storable))
                return;

            var line = _storableToLineMap[storable];
            _storableToLineMap.Remove(storable);
            _lines.Remove(line);

            var lineContainer = _lineToContainerMap[line];
            _lineToContainerMap.Remove(line);
            GameObject.Destroy(lineContainer);
        }

        public void UpdateCurve(IStorableAnimationCurve storable)
        {
            UICurveLine line;
            if (!_storableToLineMap.TryGetValue(storable, out line))
                return;

            line.SetPointsFromCurve();
            BindPoints(line);
        }

        public void SetScrubber(float time)
        {
            foreach (var line in _lines)
                line.SetScrubber(time);
        }

        public void SetScrubber(IStorableAnimationCurve storable, float time)
        {
            UICurveLine line;
            if (!_storableToLineMap.TryGetValue(storable, out line))
                return;

            line.SetScrubber(time);
        }

        private UICurveEditorPoint CreatePoint(UICurveLine line, Vector2 position)
        {
            var point = line.CreatePoint(position);
            BindPoint(point);
            return point;
        }

        private void BindPoint(UICurveEditorPoint point)
        {
            if (point == null)
                return;

            point.OnDragBegin -= OnPointBeginDrag;
            point.OnDragging -= OnPointDragging;
            point.OnClick -= OnPointClick;

            point.OnDragBegin += OnPointBeginDrag;
            point.OnDragging += OnPointDragging;
            point.OnClick += OnPointClick;
        }

        private void BindPoints(UICurveLine line)
        {
            foreach (var point in line.points)
                BindPoint(point);
        }

        private void DestroyPoint(UICurveEditorPoint point)
        {
            point?.owner?.DestroyPoint(point);
            point?.owner?.Update();
        }

        private void SetSelectedPoint(UICurveEditorPoint point)
        {
            foreach (var line in _lines)
                line.SetSelectedPoint(null);

            if (point != null)
            {
                _lineToContainerMap[point.owner].transform.SetAsLastSibling();
                point.owner.SetSelectedPoint(point);
            }
            _selectedPoint = point;
        }

        private void SetHandleMode(UICurveEditorPoint point, int mode) => point?.owner?.SetHandleMode(point, mode);
        private void SetOutHandleMode(UICurveEditorPoint point, int mode) => point?.owner?.SetOutHandleMode(point, mode);
        private void SetInHandleMode(UICurveEditorPoint point, int mode) => point?.owner?.SetInHandleMode(point, mode);

        private void OnInput(object sender, InputEventArgs e)
        {
            if (_selectedPoint != null)
            {
                if (e.Pressed && e.Key == KeyCode.Delete)
                {
                    DestroyPoint(_selectedPoint);
                    SetSelectedPoint(null);
                }
            }
        }

        private void OnLinesContainerClick(object sender, PointerEventArgs e)
        {
            if (e.Data.dragging)
                return;

            if (e.Data.clickCount > 0 && e.Data.clickCount % 2 == 0)
            {
                var rectTransform = _linesContainer.GetComponent<RectTransform>();

                Vector2 localPosition;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.Data.position, e.Data.pressEventCamera, out localPosition))
                    return;

                var pointPosition = localPosition + rectTransform.sizeDelta / 2;
                var normalizedPosition = pointPosition / rectTransform.sizeDelta;
                var closestLine = _lines.OrderBy(l => Mathf.Abs(normalizedPosition.y - l.curve.Evaluate(normalizedPosition.x))).FirstOrDefault();

                SetSelectedPoint(null);
                CreatePoint(closestLine, pointPosition);

                closestLine.Update();
            }

            SetSelectedPoint(null);
        }

        private void OnLinesContainerDrag(object sender, PointerEventArgs e)
        {
            if (_selectedPoint?.OnDrag(e.Data) == false)
                SetSelectedPoint(null);
        }

        private void OnLinesContainerEndDrag(object sender, PointerEventArgs e)
        {
            if (_selectedPoint?.OnEndDrag(e.Data) == false)
                SetSelectedPoint(null);
        }

        private void OnPointBeginDrag(object sender, UICurveEditorPoint.EventArgs e)
        {
            var point = sender as UICurveEditorPoint;
            if (_selectedPoint != point)
                SetSelectedPoint(point);
        }

        private void OnPointDragging(object sender, UICurveEditorPoint.EventArgs e) => _selectedPoint?.owner?.Update();

        private void OnPointClick(object sender, UICurveEditorPoint.EventArgs e)
        {
            var point = sender as UICurveEditorPoint;
            if (!e.Data.dragging && e.IsPointEvent)
                SetSelectedPoint(point);
        }

        public void ToggleHandleMode()
        {
            if (_selectedPoint != null)
            {
                SetHandleMode(_selectedPoint, 1 - _selectedPoint.handleMode);
                _selectedPoint.owner.Update();
            }
        }

        public void ToggleOutHandleMode()
        {
            if (_selectedPoint != null)
            {
                SetOutHandleMode(_selectedPoint, 1 - _selectedPoint.outHandleMode);
                _selectedPoint.owner.Update();
            }
        }

        public void ToggleInHandleMode()
        {
            if (_selectedPoint != null)
            {
                SetInHandleMode(_selectedPoint, 1 - _selectedPoint.inHandleMode);
                _selectedPoint.owner.Update();
            }
        }

        public void SetLinear()
        {
            if (_selectedPoint == null)
                return;

            var line = _selectedPoint.owner;

            var idx = line.points.IndexOf(_selectedPoint);
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
        }
    }
}
