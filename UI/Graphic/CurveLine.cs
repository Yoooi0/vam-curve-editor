using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using CurveEditor.Utils;
using Leap;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class CurveLine : AbstractDrawable
    {
        private readonly IStorableAnimationCurve _storable;
        private readonly UICurveLineColors _colors;
        private int _evaluateCount;
        private CurveEditorPoint _selectedPoint;

        public readonly List<CurveEditorPoint> points;
        public int evaluateCount;

        public AnimationCurve curve => _storable.val;


        public CurveLine(IStorableAnimationCurve storable, UICurveLineColors colors = null)
        {
            points = new List<CurveEditorPoint>();

            _storable = storable;
            _colors = colors ?? new UICurveLineColors();
            _evaluateCount = 200;

            SetPointsFromCurve();
        }

        public override void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Bounds viewBounds)
        {
            var curvePoints = new List<Vector2>();
            var from = Mathf.Min(viewBounds.min.x, curve.keys.First().time);
            var to = Mathf.Max(viewBounds.max.x, curve.keys.Last().time);
            for (var i = 0; i < _evaluateCount; i++)
            {
                var t = Mathf.Lerp(from, to, (float)i / (_evaluateCount - 1));
                curvePoints.Add(new Vector2(t, curve.Evaluate(t)));
            }

            vh.DrawLine(curvePoints, 0.04f, _colors.lineColor, viewMatrix);
            foreach(var point in this.points)
                point.PopulateMesh(vh, viewMatrix, viewBounds);
        }

        public void SetCurveFromPoints()
        {
            points.Sort(new UICurveEditorPointComparer());
            while (curve.length > points.Count)
                curve.RemoveKey(0);

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                var position = point.position;

                var key = new Keyframe(position.x, position.y);
                key.weightedMode = (WeightedMode)(point.inHandleMode | point.outHandleMode << 1);

                var outPosition = point.outHandlePosition;
                var inPosition = point.inHandlePosition;

                if (Mathf.Abs(inPosition.x) < 0.0001f)
                {
                    key.inTangent = Mathf.Infinity;
                    key.inWeight = 0f;
                }
                else
                {
                    key.inTangent = inPosition.y / inPosition.x;

                    var prev = i > 0 ? points[i - 1] : null;
                    if (prev != null)
                    {
                        var prevPosition = prev.position;
                        var dx = position.x - prevPosition.x;
                        key.inWeight = Mathf.Clamp(Mathf.Abs(inPosition.x / dx), 0f, 1f);
                    }
                }

                if (Mathf.Abs(outPosition.x) < 0.0001f)
                {
                    key.outTangent = Mathf.Infinity;
                    key.outWeight = 0f;
                }
                else
                {
                    key.outTangent = outPosition.y / outPosition.x;

                    var next = i < points.Count - 1 ? points[i + 1] : null;
                    if (next != null)
                    {
                        var nextPosition = next.position;
                        var dx = nextPosition.x - position.x;
                        key.outWeight = Mathf.Clamp(Mathf.Abs(outPosition.x / dx), 0f, 1f);
                    }
                }

                if (i >= curve.length)
                    curve.AddKey(key);
                else
                    curve.MoveKey(i, key);
            }

            _storable.NotifyUpdated();
        }

        public void SetPointsFromCurve()
        {
            while (points.Count > curve.length)
                DestroyPoint(points[0]);
            while (points.Count < curve.length)
                CreatePoint(new Vector2());

            for (var i = 0; i < curve.length; i++)
            {
                var point = points[i];
                var key = curve[i];
                point.position = new Vector2(key.time, key.value);

                if (key.inTangent != key.outTangent)
                    point.handleMode = 1;

                if (((int)key.weightedMode & 1) > 0) point.inHandleMode = 1;
                if (((int)key.weightedMode & 2) > 0) point.outHandleMode = 1;

                var outHandleNormal = (MathUtils.VectorFromAngle(Mathf.Atan(key.outTangent))).normalized;
                if (point.outHandleMode == 1 && i < curve.length - 1)
                {
                    var x = key.outWeight * (curve[i + 1].time - key.time);
                    var y = x * (outHandleNormal.y / outHandleNormal.x);
                    var length = Mathf.Sqrt(x * x + y * y);
                    point.outHandlePosition = outHandleNormal * length;
                }
                else
                {
                    point.outHandlePosition = outHandleNormal * point.outHandleLength;
                }

                var inHandleNormal = -(MathUtils.VectorFromAngle(Mathf.Atan(key.inTangent))).normalized;
                if (point.inHandleMode == 1 && i > 0)
                {
                    var x = key.inWeight * (key.time - curve[i - 1].time);
                    var y = x * (inHandleNormal.y / inHandleNormal.x);
                    var length = Mathf.Sqrt(x * x + y * y);
                    point.inHandlePosition = inHandleNormal * length;
                }
                else
                {
                    point.inHandlePosition = inHandleNormal * point.inHandleLength;
                }

                SetHandleMode(point, point.handleMode);
                SetOutHandleMode(point, point.outHandleMode);
                SetInHandleMode(point, point.inHandleMode);
            }
        }

        public CurveEditorPoint CreatePoint(Vector2 position)
        {
            var point = new CurveEditorPoint();
            point.owner = this;
            point.pointColor = _colors.pointColor;
            point.inHandleColor = _colors.inHandleColor;
            point.outHandleColor = _colors.outHandleColor;
            point.lineColor = _colors.handleLineColor;
            point.position = position;

            points.Add(point);
            return point;
        }

        public void DestroyPoint(CurveEditorPoint point)
        {
            if (points.Count > 1)
                points.Remove(point);
        }

        public void SetSelectedPoint(CurveEditorPoint point)
        {
            if (_selectedPoint != null)
            {
                _selectedPoint.pointColor = _colors.pointColor;
                _selectedPoint.showHandles = false;
                _selectedPoint = null;
            }

            if (point != null)
            {
                point.pointColor = _colors.selectedPointColor;
                point.showHandles = true;
                _selectedPoint = point;
            }
        }

        public void SetHandleMode(CurveEditorPoint point, int mode)
        {
            point.handleMode = mode;
            point.lineColor = mode == 0 ? _colors.handleLineColor : _colors.handleLineColorFree;
        }

        public void SetOutHandleMode(CurveEditorPoint point, int mode)
        {
            point.outHandleMode = mode;
            point.outHandleColor = mode == 0 ? _colors.outHandleColor : _colors.outHandleColorWeighted;
        }

        public void SetInHandleMode(CurveEditorPoint point, int mode)
        {
            point.inHandleMode = mode;
            point.inHandleColor = mode == 0 ? _colors.inHandleColor : _colors.inHandleColorWeighted;
        }

        private class UICurveEditorPointComparer : IComparer<CurveEditorPoint>
        {
            public int Compare(CurveEditorPoint x, CurveEditorPoint y)
                => Comparer<float>.Default.Compare(x.position.x, y.position.x);
        }
    }
}

