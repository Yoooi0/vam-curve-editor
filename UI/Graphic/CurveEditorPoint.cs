using CurveEditor.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class CurveEditorPoint
    {
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

        public CurveLine parent { get; private set; } = null;
        public UICurveLineSettings settings { get; private set; }

        public Vector2 position { get; set; } = Vector2.zero;

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

        public CurveEditorPoint(CurveLine parent, UICurveLineSettings settings)
        {
            this.parent = parent;
            this.settings = settings;

            _inHandleLength = settings.defaultHandleLength;
            _outHandleLength = settings.defaultHandleLength;
        }

        public void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Rect viewBounds)
        {
            var pointColor = _showHandles ? settings.pointDotColorSelected : settings.pointDotColor;
            var lineColor = _handleMode == 0 ? settings.pointHandleLineColor : settings.pointHandleLineColorFree;
            var handleColor = _outHandleMode == 0 ? settings.pointHandleDotColor : settings.pointHandleDotColorWeighted;

            if (showHandles)
            {
                var bounds = MathUtils.CenterSizeRect(position, 2 * Vector2.one * (settings.pointDotRadius + settings.pointDotSkin));
                bounds = bounds.Encapsulate(MathUtils.CenterSizeRect(position + _inHandlePosition, 2 * Vector2.one * (settings.pointHandleDotRadius + settings.pointHandleDotSkin)));
                bounds = bounds.Encapsulate(MathUtils.CenterSizeRect(position + _outHandlePosition, 2 * Vector2.one * (settings.pointHandleDotRadius + settings.pointHandleDotSkin)));

                if (!viewBounds.Overlaps(bounds))
                    return;
            }
            else
            {
                if (!viewBounds.Overlaps(new Rect(position - Vector2.one * settings.pointDotRadius, 2 * Vector2.one * settings.pointDotRadius)))
                    return;
            }

            if (showHandles)
            {
                vh.AddLine(position, position + _outHandlePosition, settings.pointHandleLineThickness, lineColor, viewMatrix);
                vh.AddLine(position, position + _inHandlePosition, settings.pointHandleLineThickness, lineColor, viewMatrix);
            }

            vh.AddCircle(position, settings.pointDotRadius, pointColor, viewMatrix);

            if (showHandles)
            {
                vh.AddCircle(position + _outHandlePosition, settings.pointHandleDotRadius, handleColor, viewMatrix);
                vh.AddCircle(position + _inHandlePosition, settings.pointHandleDotRadius, handleColor, viewMatrix);
            }
        }

        public bool OnBeginDrag(Vector2 point)
        {
            if (Vector2.Distance(point, position) <= settings.pointDotRadius + settings.pointDotSkin)
            {
                _isDraggingPoint = true;
                position = point;
            }

            if (showHandles)
            {
                if (Vector3.Distance(point, position + _outHandlePosition) <= settings.pointHandleDotRadius + settings.pointHandleDotSkin)
                {
                    _isDraggingOutHandle = true;
                    SetOutHandlePosition(point - position);
                }
                else if (Vector2.Distance(point, position + _inHandlePosition) <= settings.pointHandleDotRadius + settings.pointHandleDotSkin)
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
            if (Vector2.Distance(point, position) <= settings.pointDotRadius + settings.pointDotSkin) return true;

            if (!showHandles) return false;
            if (Vector2.Distance(point, position + _outHandlePosition) <= settings.pointHandleDotRadius + settings.pointHandleDotSkin) return true;
            if (Vector2.Distance(point, position + _inHandlePosition) <= settings.pointHandleDotRadius + settings.pointHandleDotSkin) return true;

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
            return MathUtils.DistanceToLine(point, position, position + _inHandlePosition) > settings.pointShellSize
                && MathUtils.DistanceToLine(point, position, position + _outHandlePosition) > settings.pointShellSize;
        }
    }
}
