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
        private readonly GameObject _canvasContent;

        private readonly float _width;
        private readonly float _height;
        private readonly UIColors _colors;
        private readonly List<UICurveLine> _lines = new List<UICurveLine>();
        private UICurveEditorPoint _selectedPoint;

        public UICurveLine AddCurve(AnimationCurve curve, Color? color = null, float thickness = 4)
        {
            var line = _canvasContent.AddComponent<UILine>();
            line.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
            line.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height - _buttonHeight);
            line.color = color ?? _colors.lineColor;
            line.lineThickness = 4;

            var curveLine = new UICurveLine(curve, line, _colors);
            _lines.Add(curveLine);
            UpdatePoints(curveLine);
            return curveLine;
        }

        public void UpdatePoints(AnimationCurve curve)
        {
            var line = _lines.FirstOrDefault(l => l.curve == curve);
            if (line == null) return;
            UpdatePoints(line);
        }

        private void UpdatePoints(UICurveLine line)
        {
            foreach (var point in line.SetPointsFromCurve())
            {
                BindPoint(point);
            }
        }

        public UICurveEditor(IUIBuilder builder, UIDynamic container, float width, float height)
        {
            this.container = container;

            _width = width;
            _height = height;

            _colors = new UIColors();

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, _buttonHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height - _buttonHeight);

            var input = gameObject.AddComponent<UIInputBehaviour>();
            input.OnInput += OnInput;

            var backgroundContent = new GameObject();
            _canvasContent = new GameObject();

            backgroundContent.transform.SetParent(gameObject.transform, false);
            _canvasContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _height - _buttonHeight);
            backgroundImage.color = _colors.backgroundColor;

            var mouseClick = _canvasContent.AddComponent<UIMouseClickBehaviour>();
            mouseClick.OnClick += OnCanvasClick;

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

        private UICurveEditorPoint CreatePoint(Vector2 position)
        {
            var line = _lines.FirstOrDefault();
            if (line == null) return null;
            var point = line.CreatePoint(position);
            BindPoint(point);
            return point;
        }

        private void BindPoint(UICurveEditorPoint point)
        {
            point.OnDragBegin += OnPointBeginDrag;
            point.OnDragging += OnPointDragging;
            point.OnClick += OnPointClick;
        }

        private void DestroyPoint(UICurveEditorPoint point)
        {
            point.owner.DestroyPoint(point);
        }

        private void SetSelectedPoint(UICurveEditorPoint point)
        {
            if (_selectedPoint != null)
            {
                _selectedPoint.color = _colors.pointColor;
                _selectedPoint.showHandles = false;
                _selectedPoint = null;
            }

            if (point != null)
            {
                point.color = _colors.selectedPointColor;
                point.showHandles = true;
                point.SetVerticesDirty();

                _selectedPoint = point;
            }
        }

        private void SetHandleMode(UICurveEditorPoint point, int mode)
        {
            if (point == null)
                return;

            point.handleMode = mode;
            point.lineColor = mode == 0 ? _colors.handleLineColor : _colors.handleLineColorFree;
        }

        private void SetOutHandleMode(UICurveEditorPoint point, int mode)
        {
            if (point == null)
                return;

            point.outHandleMode = mode;
            point.outHandleColor = mode == 0 ? _colors.outHandleColor : _colors.outHandleColorWeighted;
        }

        private void SetInHandleMode(UICurveEditorPoint point, int mode)
        {
            if (point == null)
                return;

            point.inHandleMode = mode;
            point.inHandleColor = mode == 0 ? _colors.inHandleColor : _colors.inHandleColorWeighted;
        }
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
                    else if (e.Key == KeyCode.Z)
                    {
                        SetInHandleMode(_selectedPoint, 1 - _selectedPoint.inHandleMode);
                        _selectedPoint.owner.UpdateCurve();
                    }
                    else if (e.Key == KeyCode.X)
                    {
                        SetOutHandleMode(_selectedPoint, 1 - _selectedPoint.outHandleMode);
                        _selectedPoint.owner.UpdateCurve();
                    }
                    else if (e.Key == KeyCode.C)
                    {
                        SetHandleMode(_selectedPoint, 1 - _selectedPoint.handleMode);
                        _selectedPoint.owner.UpdateCurve();
                    }
                }
            }
        }

        private void OnCanvasClick(object sender, PointerEventArgs e)
        {
            if (e.Data.clickCount == 2)
            {
                var line = _lines.Single();

                Vector2 localPosition;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(line.line.rectTransform, e.Data.position, e.Data.pressEventCamera, out localPosition))
                    return;

                CreatePoint(localPosition + line.line.rectTransform.sizeDelta / 2);
                line.UpdateCurve();
                SetSelectedPoint(null);
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

        private void OnPointDragging(object sender, UICurveEditorPoint.EventArgs e) => _selectedPoint.owner.UpdateCurve();

        private void OnPointClick(object sender, UICurveEditorPoint.EventArgs e)
        {
            var point = sender as UICurveEditorPoint;
            if (!e.Data.dragging)
            {
                if (e.IsPointEvent)
                    SetSelectedPoint(point);
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
                _selectedPoint.owner.UpdateCurve();
            }
        }

        private void OnOutHandleModeButtonClick()
        {
            if (_selectedPoint != null)
            {
                SetOutHandleMode(_selectedPoint, 1 - _selectedPoint.outHandleMode);
                _selectedPoint.owner.UpdateCurve();
            }
        }

        private void OnInHandleModeButtonClick()
        {
            if (_selectedPoint != null)
            {
                SetInHandleMode(_selectedPoint, 1 - _selectedPoint.inHandleMode);
                _selectedPoint.owner.UpdateCurve();
            }
        }

        private void OnSetLinearButtonClick()
        {
            if (_selectedPoint == null)
                return;

            var line = _selectedPoint.owner;

            var idx = line.points.IndexOf(_selectedPoint);
            var curve = line.curve;
            var key = curve.keys[idx];

            if (idx > 0)
            {
                var prev = curve.keys[idx - 1];
                prev.outTangent = key.inTangent = (key.value - prev.value) / (key.time - prev.time);
                curve.MoveKey(idx - 1, prev);
            }

            if (idx < curve.keys.Length - 1)
            {
                var next = curve.keys[idx + 1];
                next.inTangent = key.outTangent = (next.value - key.value) / (next.time - key.time);
                curve.MoveKey(idx + 1, next);
            }

            curve.MoveKey(idx, key);
            foreach (var point in line.SetPointsFromCurve())
            {
                BindPoint(point);
            }
        }

        private bool IsClickOutsidePoint(UICurveEditorPoint point, PointerEventData eventData)
        {
            if (point == null)
                return false;

            var line = point.owner;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(line.line.rectTransform, eventData.pressPosition, eventData.pressEventCamera, out localPoint))
            {
                var p = localPoint + line.line.rectTransform.sizeDelta / 2;
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
