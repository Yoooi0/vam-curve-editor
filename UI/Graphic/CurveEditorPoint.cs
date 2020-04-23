using System;
using CurveEditor.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class CurveEditorPoint
    {
        private readonly UICurveLineColors _colors; //TODO: just point colors

        private float _pointRadius = 0.08f;
        private float _pointSkin = 0.04f;
        private float _handleRadius = 0.07f;
        private float _handleSkin = 0.04f;
        private float _shellSize = 0.2f;
        private float _handleThickness = 0.03f;
        private float _outHandleLength = 0.5f;
        private float _inHandleLength = 0.5f;
        private bool _isDraggingPoint = false;
        private bool _isDraggingOutHandle = false;
        private bool _isDraggingInHandle = false;
        private bool _showHandles = false;
        private int _handleMode = 0;    // 0 = both, 1 = free
        private int _inHandleMode = 0;  // 0 = constant, 1 = weighted
        private int _outHandleMode = 0; // 0 = constant, 1 = weighted

        private Vector2 _outHandlePosition = Vector2.right * 0.5f;
        private Vector2 _inHandlePosition = Vector2.left * 0.5f;
        private Color pointColor = new Color(1, 1, 1);
        private Color lineColor = new Color(0.5f, 0.5f, 0.5f);
        private Color inHandleColor = new Color(0, 0, 0);
        private Color outHandleColor = new Color(0, 0, 0);

        public CurveLine parent { get; private set; } = null;

        public Vector2 position { get; set; } = Vector2.zero;
        public bool showHandles
        {
            get { return _showHandles; }
            set
            {
                _showHandles = value;
                pointColor = _showHandles ? _colors.selectedPointColor : _colors.pointColor;
            }
        }

        public int handleMode
        {
            get { return _handleMode; }
            set { SetHandleMode(value); }
        }

        public int inHandleMode
        {
            get { return _inHandleMode; }
            set { SetInHandleMode(value); }
        }

        public int outHandleMode
        {
            get { return _outHandleMode; }
            set { SetOutHandleMode(value); }
        }

        public float outHandleLength
        {
            get { return _outHandleLength; }
            set { _outHandleLength = value; SetOutHandlePosition(_outHandlePosition); }
        }

        public float inHandleLength
        {
            get { return _inHandleLength; }
            set { _inHandleLength = value; SetInHandlePosition(_inHandlePosition); }
        }

        public Vector2 outHandlePosition
        {
            get { return _outHandlePosition; }
            set { SetOutHandlePosition(value); }
        }

        public Vector2 inHandlePosition
        {
            get { return _inHandlePosition; }
            set { SetInHandlePosition(value); }
        }

        public CurveEditorPoint(CurveLine parent, UICurveLineColors colors = null)
        {
            this.parent = parent;
            _colors = colors ?? new UICurveLineColors();

            pointColor = _colors.pointColor;
            inHandleColor = _colors.inHandleColor;
            outHandleColor = _colors.outHandleColor;
            lineColor = _colors.handleLineColor;
        }

        public void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Bounds viewBounds)
        {
            if (showHandles)
            {
                var center = position + (_inHandlePosition + _outHandlePosition) / 2;
                var size = _outHandlePosition.normalized * (_outHandlePosition.magnitude + _handleRadius)
                         - _inHandlePosition.normalized * (_inHandlePosition.magnitude + _handleRadius);

                var bounds = new Bounds(center, new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y)));
                if (!viewBounds.Intersects(bounds))
                    return;
            }
            else
            {
                if (!viewBounds.Intersects(new Bounds(position, 2 * Vector2.one * _pointRadius)))
                    return;
            }

            if (showHandles)
            {
                vh.AddLine(position, position + _outHandlePosition, _handleThickness, lineColor, viewMatrix);
                vh.AddLine(position, position + _inHandlePosition, _handleThickness, lineColor, viewMatrix);
            }

            vh.AddCircle(position, _pointRadius, pointColor, viewMatrix);

            if (showHandles)
            {
                vh.AddCircle(position + _outHandlePosition, _handleRadius, outHandleColor, viewMatrix);
                vh.AddCircle(position + _inHandlePosition, _handleRadius, inHandleColor, viewMatrix);
            }
        }

        public bool OnBeginDrag(Vector2 point)
        {
            if (Vector2.Distance(point, position) <= _pointRadius + _pointSkin)
            {
                _isDraggingPoint = true;
                position = point;
            }

            if (showHandles)
            {
                if (Vector3.Distance(point, position + _outHandlePosition) <= _handleRadius + _handleSkin)
                {
                    _isDraggingOutHandle = true;
                    SetOutHandlePosition(point - position);
                }
                else if (Vector2.Distance(point, position + _inHandlePosition) <= _handleRadius + _handleSkin)
                {
                    _isDraggingInHandle = true;
                    SetInHandlePosition(point - position);
                }
            }

            return _isDraggingPoint || _isDraggingOutHandle || _isDraggingInHandle;
        }

        public bool OnDrag(Vector2 point)
        {
            if (!_isDraggingPoint && !_isDraggingOutHandle && !_isDraggingInHandle)
                return false;

            if (_isDraggingPoint) position = point;
            else if (_isDraggingOutHandle) SetOutHandlePosition(point - position);
            else if (_isDraggingInHandle) SetInHandlePosition(point - position);

            return true;
        }

        public bool OnEndDrag(Vector2 _)
        {
            if (!_isDraggingPoint && !_isDraggingOutHandle && !_isDraggingInHandle)
                return false;

            _isDraggingPoint = false;
            _isDraggingOutHandle = false;
            _isDraggingInHandle = false;

            return true;
        }

        public bool OnPointerClick(Vector2 point)
        {
            if (Vector2.Distance(point, position) <= _pointRadius + _pointSkin) return true;

            if (!showHandles) return false;
            if (Vector2.Distance(point, position + _outHandlePosition) <= _handleRadius + _handleSkin) return true;
            if (Vector2.Distance(point, position + _inHandlePosition) <= _handleRadius + _handleSkin) return true;

            return !IsPositionOutsideShell(point);
        }

        private void SetHandleMode(int mode)
        {
            _handleMode = mode;
            lineColor = mode == 0 ? _colors.handleLineColor : _colors.handleLineColorFree;
            SetOutHandlePosition(_outHandlePosition);
        }

        private void SetOutHandleMode(int mode)
        {
            _outHandleMode = mode;
            outHandleColor = mode == 0 ? _colors.outHandleColor : _colors.outHandleColorWeighted;
            SetOutHandlePosition(_outHandlePosition);
        }

        private void SetInHandleMode(int mode)
        {
            _inHandleMode = mode;
            inHandleColor = mode == 0 ? _colors.inHandleColor : _colors.inHandleColorWeighted;
            SetInHandlePosition(_inHandlePosition);
        }


        private void SetOutHandlePosition(Vector2 handlePosition)
        {
            if (handlePosition.x < 0)
                _outHandlePosition = Vector2.up * _outHandleLength;
            else
                _outHandlePosition = handlePosition.normalized * (_outHandleMode == 1 ? handlePosition.magnitude : _outHandleLength);

            if (_handleMode == 0)
            {
                if (_inHandleMode == 0 || handlePosition.x < 0)
                    _inHandlePosition = -_outHandlePosition.normalized * _inHandleLength;
                else
                    _inHandlePosition = -_outHandlePosition.normalized * _inHandlePosition.magnitude;
            }
        }

        private void SetInHandlePosition(Vector2 handlePosition)
        {
            if (handlePosition.x > 0)
                _inHandlePosition = Vector2.down * _inHandleLength;
            else
                _inHandlePosition = handlePosition.normalized * (_inHandleMode == 1 ? handlePosition.magnitude : _inHandleLength);

            if (_handleMode == 0)
            {
                if (_outHandleMode == 0 || handlePosition.x > 0)
                    _outHandlePosition = -_inHandlePosition.normalized * _outHandleLength;
                else
                    _outHandlePosition = -_inHandlePosition.normalized * _outHandlePosition.magnitude;
            }
        }

        public bool IsPositionOutsideShell(Vector2 point)
        {
            return MathUtils.DistanceToLine(point, position, position + _inHandlePosition) > _shellSize
                && MathUtils.DistanceToLine(point, position, position + _outHandlePosition) > _shellSize;
        }
    }
}
