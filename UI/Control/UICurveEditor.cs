using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class UICurveEditor
    {
        public readonly UIDynamic container;
        public readonly GameObject gameObject;

        private readonly GameObject _canvasContainer;
        private readonly UICurveEditorCanvas _canvas;
        private readonly UICurveEditorColors _colors;

        private readonly Dictionary<IStorableAnimationCurve, CurveLine> _storableToLineMap;
        private bool _readOnly;

        public bool readOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;

                var canvasGroup = _canvasContainer.GetComponent<CanvasGroup>();
                canvasGroup.interactable = !value;
                canvasGroup.blocksRaycasts = !value;

                _canvas.readOnly = value;
                _canvas.SetSelectedPoint(null);
            }
        }

        public bool allowKeyboardShortcuts
        {
            get { return _canvas.allowKeyboardShortcuts; }
            set { _canvas.allowKeyboardShortcuts = value; }
        }

        public bool showScrubbers
        {
            get { return _canvas.showScrubbers; }
            set { _canvas.showScrubbers = value; }
        }

        public bool allowViewDragging
        {
            get { return _canvas.allowViewDragging; }
            set { _canvas.allowViewDragging = value; }
        }

        public bool allowViewZooming
        {
            get { return _canvas.allowViewZooming; }
            set { _canvas.allowViewZooming = value; }
        }

        public UICurveEditor(UIDynamic container, float width, float height, List<UIDynamicButton> buttons = null, UICurveEditorColors colors = null, bool readOnly = false)
        {
            var buttonContainerHeight = (buttons == null || buttons.Count == 0) ? 0 : 25;

            this.container = container;

            _storableToLineMap = new Dictionary<IStorableAnimationCurve, CurveLine>();
            _colors = colors ?? new UICurveEditorColors();

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, buttonContainerHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            var backgroundContent = new GameObject();
            backgroundContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);
            backgroundImage.color = _colors.backgroundColor;

            _canvasContainer = new GameObject();
            _canvasContainer.transform.SetParent(gameObject.transform, false);
            _canvasContainer.AddComponent<CanvasGroup>();
            _canvas = _canvasContainer.AddComponent<UICurveEditorCanvas>();
            _canvas.rectTransform.anchorMin = new Vector2(0, 0);
            _canvas.rectTransform.anchorMax = new Vector2(0, 0);
            _canvas.rectTransform.pivot = new Vector2(0, 0);
            _canvas.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _canvas.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            this.readOnly = readOnly;

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
        }

        public CurveLine AddCurve(IStorableAnimationCurve storable, UICurveLineColors colors = null, float thickness = 4)
        {
            var curveLine = _canvas.CreateCurve(storable, colors, thickness);
            _storableToLineMap.Add(storable, curveLine);
            return curveLine;
        }

        public void RemoveCurve(IStorableAnimationCurve storable)
        {
            if (!_storableToLineMap.ContainsKey(storable))
                return;

            var line = _storableToLineMap[storable];
            _canvas.RemoveCurve(line);
            _storableToLineMap.Remove(storable);
        }

        public void UpdateCurve(IStorableAnimationCurve storable)
        {
            CurveLine line;
            if (!_storableToLineMap.TryGetValue(storable, out line))
                return;

            line.SetPointsFromCurve();
            _canvas.SetVerticesDirty();
        }

        public void SetScrubber(float time)
        {
            foreach(var kv in _storableToLineMap)
                _canvas.SetScrubberPosition(kv.Value, time);
        }

        public void SetScrubber(IStorableAnimationCurve storable, float time)
        {
            if (!_storableToLineMap.ContainsKey(storable))
                return;

            _canvas.SetScrubberPosition(_storableToLineMap[storable], time);
        }

        public void SetViewToFit() => _canvas.SetViewToFit();

        public void ToggleHandleMode()
        {
            if (_canvas.selectedPoint != null)
            {
                var line = _canvas.selectedPoint.parent;
                line.SetHandleMode(_canvas.selectedPoint, 1 - _canvas.selectedPoint.handleMode);
                line.SetCurveFromPoints();
                _canvas.SetVerticesDirty();
            }
        }

        public void ToggleOutHandleMode()
        {
            if (_canvas.selectedPoint != null)
            {
                var line = _canvas.selectedPoint.parent;
                line.SetOutHandleMode(_canvas.selectedPoint, 1 - _canvas.selectedPoint.outHandleMode);
                line.SetCurveFromPoints();
                _canvas.SetVerticesDirty();
            }
        }

        public void ToggleInHandleMode()
        {
            if (_canvas.selectedPoint != null)
            {
                var line = _canvas.selectedPoint.parent;
                line.SetInHandleMode(_canvas.selectedPoint, 1 - _canvas.selectedPoint.inHandleMode);
                line.SetCurveFromPoints();
                _canvas.SetVerticesDirty();
            }
        }

        public void SetLinear()
        {
            if (_canvas.selectedPoint == null)
                return;

            var line = _canvas.selectedPoint.parent;
            var idx = line.points.IndexOf(_canvas.selectedPoint);
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
            _canvas.SetVerticesDirty();
        }
    }
}
