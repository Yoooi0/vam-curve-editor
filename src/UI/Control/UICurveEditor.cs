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
        private const float _buttonHeight = 25;

        public readonly UIDynamic container;
        public readonly GameObject gameObject;

        private readonly float _width;
        private readonly float _height;
        private readonly UICurveEditorColors _colors;
        private readonly List<UICurveLine> _lines;
        private readonly Dictionary<IStorableAnimationCurve, UICurveLine> _storableToLineMap;
        private readonly Dictionary<UICurveLine, GameObject> _lineToContainerMap;
        private UICurveEditorPoint _selectedPoint;

        private GameObject _linesContainer;

        public UICurveEditor(IUIBuilder builder, UIDynamic container, float width, float height, UICurveEditorColors colors = null)
        {
            this.container = container;

            _storableToLineMap = new Dictionary<IStorableAnimationCurve, UICurveLine>();
            _lineToContainerMap = new Dictionary<UICurveLine, GameObject>();
            _lines = new List<UICurveLine>();
            _colors = colors ?? new UICurveEditorColors();

            _width = width;
            _height = height;

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, _buttonHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height - _buttonHeight);

            var input = gameObject.AddComponent<UIInputBehaviour>();
            input.OnInput += OnInput;

            var backgroundContent = new GameObject();
            backgroundContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height - _buttonHeight);
            backgroundImage.color = _colors.backgroundColor;

            _linesContainer = new GameObject();
            _linesContainer.transform.SetParent(gameObject.transform, false);
            var lineContainerRectTranform = _linesContainer.AddComponent<RectTransform>();
            lineContainerRectTranform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
            lineContainerRectTranform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height - _buttonHeight);

            var buttonGroup = new UIHorizontalGroup(container, 510, _buttonHeight, new Vector2(0, 0), 4, idx => builder.CreateButtonEx());
            buttonGroup.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(_height - _buttonHeight) / 2);
            var buttons = buttonGroup.items.Select(o => o.GetComponent<UIDynamicButton>()).ToList();

            foreach (var b in buttons)
            {
                b.buttonText.fontSize = 18;
                b.buttonColor = Color.white;
            }

            buttons[0].label = "Mode";
            buttons[1].label = "In Mode";
            buttons[2].label = "Out Mode";
            buttons[3].label = "Linear";

            buttons[0].button.onClick.AddListener(OnHandleModeButtonClick);
            buttons[1].button.onClick.AddListener(OnInHandleModeButtonClick);
            buttons[2].button.onClick.AddListener(OnOutHandleModeButtonClick);
            buttons[3].button.onClick.AddListener(OnSetLinearButtonClick);
        }

        public UICurveLine AddCurve(IStorableAnimationCurve storable, UICurveLineColors colors = null, float thickness = 4)
        {
            var lineContainer = new GameObject();
            lineContainer.transform.SetParent(_linesContainer.transform, false);

            if (_lines.Count == 0)
            {
                var mouseClick = lineContainer.AddComponent<UIMouseClickBehaviour>();
                mouseClick.OnClick += OnCanvasClick;
            }

            var rectTransform = _linesContainer.GetComponent<RectTransform>();
            var line = lineContainer.AddComponent<UILine>();
            line.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.sizeDelta.x);
            line.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y);
            line.lineThickness = thickness;

            var curveLine = new UICurveLine(storable, line, colors);
            _lines.Add(curveLine);
            _storableToLineMap.Add(storable, curveLine);
            _lineToContainerMap.Add(curveLine, lineContainer);

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
            foreach (var point in line.points)
                BindPoint(point);
        }

        private UICurveEditorPoint CreatePoint(Vector2 position)
        {
            var line = _lines.FirstOrDefault();
            if (line == null)
                return null;

            var point = line.CreatePoint(position);
            BindPoint(point);
            return point;
        }

        private void BindPoint(UICurveEditorPoint point)
        {
            if (point == null)
                return;

            point.OnDragBegin += OnPointBeginDrag;
            point.OnDragging += OnPointDragging;
            point.OnClick += OnPointClick;
        }

        private void DestroyPoint(UICurveEditorPoint point)
        {
            point?.owner?.DestroyPoint(point);
        }

        private void SetSelectedPoint(UICurveEditorPoint point)
        {
            foreach (var line in _lines)
                line.SetSelectedPoint(null);

            point?.owner?.SetSelectedPoint(point);
            _selectedPoint = point;
        }

        private void SetHandleMode(UICurveEditorPoint point, int mode) => point?.owner?.SetHandleMode(point, mode);
        private void SetOutHandleMode(UICurveEditorPoint point, int mode) => point?.owner?.SetOutHandleMode(point, mode);
        private void SetInHandleMode(UICurveEditorPoint point, int mode) => point?.owner?.SetInHandleMode(point, mode);

        private void OnInput(object sender, InputEventArgs e)
        {
            if (_selectedPoint != null)
            {
                if (e.Pressed)
                {
                    if (e.Key == KeyCode.Delete)
                    {
                        DestroyPoint(_selectedPoint);
                        SetSelectedPoint(null);
                    }
                }
            }
        }

        private void OnCanvasClick(object sender, PointerEventArgs e)
        {
            if (_lines.Count == 0)
                return;

            if (e.Data.clickCount > 0 && e.Data.clickCount % 2 == 0)
            {
                var line = _lines[0];
                var rectTransform = _linesContainer.GetComponent<RectTransform>();

                Vector2 localPosition;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.Data.position, e.Data.pressEventCamera, out localPosition))
                    return;

                CreatePoint(localPosition + rectTransform.sizeDelta / 2);
                SetSelectedPoint(null);

                line.Update();
            }

            if (IsClickOutsidePoint(_selectedPoint, e.Data))
                SetSelectedPoint(null);
        }

        private void OnPointBeginDrag(object sender, UICurveEditorPoint.EventArgs e)
        {
            var p = sender as UICurveEditorPoint;
            if (_selectedPoint != p)
                SetSelectedPoint(p);
        }

        private void OnPointDragging(object sender, UICurveEditorPoint.EventArgs e) => _selectedPoint?.owner?.Update();

        private void OnPointClick(object sender, UICurveEditorPoint.EventArgs e)
        {
            var point = sender as UICurveEditorPoint;
            if (!e.Data.dragging)
            {
                if (e.IsPointEvent)
                {
                    SetSelectedPoint(point);
                }
                else if (!e.IsInHandleEvent && !e.IsOutHandleEvent)
                {
                    if (IsClickOutsidePoint(point, e.Data))
                        SetSelectedPoint(null);
                }
            }
        }

        private void OnHandleModeButtonClick()
        {
            if (_selectedPoint != null)
            {
                SetHandleMode(_selectedPoint, 1 - _selectedPoint.handleMode);
                _selectedPoint.owner.Update();
            }
        }

        private void OnOutHandleModeButtonClick()
        {
            if (_selectedPoint != null)
            {
                SetOutHandleMode(_selectedPoint, 1 - _selectedPoint.outHandleMode);
                _selectedPoint.owner.Update();
            }
        }

        private void OnInHandleModeButtonClick()
        {
            if (_selectedPoint != null)
            {
                SetInHandleMode(_selectedPoint, 1 - _selectedPoint.inHandleMode);
                _selectedPoint.owner.Update();
            }
        }

        private void OnSetLinearButtonClick()
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

        private bool IsClickOutsidePoint(UICurveEditorPoint point, PointerEventData eventData)
        {
            if (point == null)
                return false;

            var rectTransform = _linesContainer.GetComponent<RectTransform>();

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.pressPosition, eventData.pressEventCamera, out localPoint))
            {
                var p = localPoint + rectTransform.sizeDelta / 2;
                var c = point.rectTransform.anchoredPosition;
                var a = c + point.inHandlePosition;
                var b = c + point.outHandlePosition;

                if (MathUtils.DistanceToLine(p, c, a) > 20 && MathUtils.DistanceToLine(p, c, b) > 20)
                    return true;
            }

            return false;
        }
    }
}
