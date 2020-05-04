using CurveEditor.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class CurveEditorPoint
    {
        private bool _isDraggingPoint = false;
        private bool _isDraggingOutHandle = false;
        private bool _isDraggingInHandle = false;
        private bool _showHandles = false;
        private int _handleMode = 0;    // 0 = both, 1 = free
        private int _inHandleMode = 0;  // 0 = constant, 1 = weighted
        private int _outHandleMode = 0; // 0 = constant, 1 = weighted

        private Vector2 _outHandlePosition = Vector2.right * 0.5f;
        private Vector2 _inHandlePosition = Vector2.left * 0.5f;

        public CurveLine parent { get; }
        public CurveLineSettings settings { get; }

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

        public CurveEditorPoint(CurveLine parent, CurveLineSettings settings)
        {
            this.parent = parent;
            this.settings = settings;
        }

        public void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Rect viewBounds)
        {
            if (showHandles)
            {
                var bounds = MathUtils.CenterSizeRect(position, 2 * Vector2.one * (settings.pointDotRadius + settings.pointDotSkin));
                bounds = bounds.Encapsulate(MathUtils.CenterSizeRect(position + _inHandlePosition, 2 * Vector2.one * (settings.pointHandleDotRadius + settings.pointHandleDotSkin)));
                bounds = bounds.Encapsulate(MathUtils.CenterSizeRect(position + _outHandlePosition, 2 * Vector2.one * (settings.pointHandleDotRadius + settings.pointHandleDotSkin)));

                if (!viewBounds.Overlaps(bounds))
                    return;
            }
            else if (!viewBounds.Overlaps(new Rect(position - Vector2.one * settings.pointDotRadius, 2 * Vector2.one * settings.pointDotRadius)))
            {
                return;
            }

            if (showHandles)
            {
                var handleLineColor = _handleMode == 0 ? settings.pointHandleLineColor : settings.pointHandleLineColorFree;
                var outHandleColor = _outHandleMode == 0 ? settings.pointHandleDotColor : settings.pointHandleDotColorWeighted;
                var inHandleColor = _inHandleMode == 0 ? settings.pointHandleDotColor : settings.pointHandleDotColorWeighted;

                vh.AddLine(position, position + _outHandlePosition, settings.pointHandleLineThickness, handleLineColor, viewMatrix);
                vh.AddLine(position, position + _inHandlePosition, settings.pointHandleLineThickness, handleLineColor, viewMatrix);

                vh.AddCircle(position + _outHandlePosition, settings.pointHandleDotRadius, outHandleColor, viewMatrix);
                vh.AddCircle(position + _inHandlePosition, settings.pointHandleDotRadius, inHandleColor, viewMatrix);
            }

            var pointColor = _showHandles ? settings.pointDotColorSelected : settings.pointDotColor;
            vh.AddCircle(position, settings.pointDotRadius, pointColor, viewMatrix);
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
                _outHandlePosition = Vector2.up * settings.defaultPointHandleLength;
            else
                _outHandlePosition = handlePosition.normalized * (_outHandleMode == 1 ? handlePosition.magnitude : settings.defaultPointHandleLength);

            if (_handleMode == 0)
                _inHandlePosition = -_outHandlePosition.normalized * ((_inHandleMode == 0 || handlePosition.x < 0) ? settings.defaultPointHandleLength : _inHandlePosition.magnitude);
        }

        private void SetInHandlePosition(Vector2 handlePosition)
        {
            if (handlePosition.x > 0)
                _inHandlePosition = Vector2.down * settings.defaultPointHandleLength;
            else
                _inHandlePosition = handlePosition.normalized * (_inHandleMode == 1 ? handlePosition.magnitude : settings.defaultPointHandleLength);

            if (_handleMode == 0)
                _outHandlePosition = -_inHandlePosition.normalized * ((_outHandleMode == 0 || handlePosition.x > 0) ? settings.defaultPointHandleLength : _outHandlePosition.magnitude);
        }

        public bool IsPositionOutsideShell(Vector2 point)
        {
            return MathUtils.DistanceToLine(point, position, position + _inHandlePosition) > settings.pointShellSize
                && MathUtils.DistanceToLine(point, position, position + _outHandlePosition) > settings.pointShellSize;
        }
    }
}
