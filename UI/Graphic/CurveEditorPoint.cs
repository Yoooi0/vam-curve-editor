using CurveEditor.Utils;
using Leap;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class CurveEditorPoint : AbstractDrawable
    {
        public CurveLine owner;

        private float _pointRadius = 0.08f;
        private float _pointSkin = 0.05f;
        private float _handleRadius = 0.07f;
        private float _handleSkin = 0.04f;
        private float _outHandleLength = 0.5f;
        private float _inHandleLength = 0.5f;
        private bool _isDraggingPoint = false;
        private bool _isDraggingOutHandle = false;
        private bool _isDraggingInHandle = false;
        private bool _showHandles = false;

        private int _handleMode = 0;    // 0 = both, 1 = free
        private int _inHandleMode = 0;  // 0 = constant, 1 = weighted
        private int _outHandleMode = 0; // 0 = constant, 1 = weighted

        private Color _pointColor = new Color(1, 1, 1);
        private Color _lineColor = new Color(0.5f, 0.5f, 0.5f);
        private Color _inHandleColor = new Color(0, 0, 0);
        private Color _outHandleColor = new Color(0, 0, 0);

        private Vector2 _outHandlePosition = Vector2.right * 0.5f;
        private Vector2 _inHandlePosition = Vector2.left * 0.5f;

        public Vector2 position;

        public float pointRadius
        {
            get { return _pointRadius; }
            set { _pointRadius = value; }
        }

        public float handlePointRadius
        {
            get { return _handleRadius; }
            set { _handleRadius = value; }
        }

        public float pointSkin
        {
            get { return _pointSkin; }
            set { _pointSkin = value; }
        }

        public float handleSkin
        {
            get { return _handleSkin; }
            set { _handleSkin = value; }
        }

        public bool showHandles
        {
            get { return _showHandles; }
            set { _showHandles = value; }
        }

        public int handleMode
        {
            get { return _handleMode; }
            set { _handleMode = value; SetOutHandlePosition(_outHandlePosition); }
        }

        public int inHandleMode
        {
            get { return _inHandleMode; }
            set { _inHandleMode = value; SetInHandlePosition(_inHandlePosition); }
        }

        public int outHandleMode
        {
            get { return _outHandleMode; }
            set { _outHandleMode = value; SetOutHandlePosition(_outHandlePosition); }
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

        public Color pointColor
        {
            get { return _pointColor; }
            set { _pointColor = value; }
        }

        public Color lineColor
        {
            get { return _lineColor; }
            set { _lineColor = value; }
        }

        public Color inHandleColor
        {
            get { return _inHandleColor; }
            set { _inHandleColor = value; }
        }

        public Color outHandleColor
        {
            get { return _outHandleColor; }
            set { _outHandleColor = value; }
        }

        protected UIVertex[] CreateVbo(Vector2[] vertices, Color color)
        {
            var vbo = new UIVertex[4];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = vertices[i];
                vbo[i] = vert;
            }
            return vbo;
        }

        public override void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Bounds viewBounds)
        {
            if (_showHandles)
            {
                vh.DrawLine(position, position + _outHandlePosition, 0.04f, _lineColor, viewMatrix);
                vh.DrawLine(position, position + _inHandlePosition, 0.04f, _lineColor, viewMatrix);
            }

            vh.DrawCircle(position, _pointRadius, _pointColor, viewMatrix);

            if (_showHandles)
            {
                vh.DrawCircle(position + _outHandlePosition, _handleRadius, _outHandleColor, viewMatrix);
                vh.DrawCircle(position + _inHandlePosition, _handleRadius, _inHandleColor, viewMatrix);
            }
        }

        public bool OnBeginDrag(Vector2 point)
        {
            if (Vector2.Distance(point, position) <= _pointRadius + _pointSkin)
            {
                _isDraggingPoint = true;
                position = point;
                return true;
            }

            if (!showHandles)
                return false;
           
            if (Vector2.Distance(point, position + _outHandlePosition) <= _handleRadius + _handleSkin)
            {
                _isDraggingOutHandle = true;
                SetOutHandlePosition(point - position);
                return true;
            }
            else if (Vector2.Distance(point, position + _inHandlePosition) <= _handleRadius + _handleSkin)
            {
                _isDraggingInHandle = true;
                SetInHandlePosition(point - position);
                return true;
            }

            return false;

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

        public bool OnEndDrag(Vector2 point)
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
            return MathUtils.DistanceToLine(point, position, position + _inHandlePosition) > 0.2f
                && MathUtils.DistanceToLine(point, position, position + _outHandlePosition) > 0.2f;
        }
    }
}
