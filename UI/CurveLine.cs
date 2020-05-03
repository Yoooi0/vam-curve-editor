using System;
using System.Collections.Generic;
using System.Linq;
using CurveEditor.Utils;
using Leap.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class CurveLine
    {
        private readonly IStorableAnimationCurve _storable;

        private CurveEditorPoint _selectedPoint;
        private DrawScaleOffset _drawScale = new DrawScaleOffset();

        public readonly List<CurveEditorPoint> points;

        public CurveLineSettings settings { get; }
        public AnimationCurve curve => _storable.val;

        public DrawScaleOffset drawScale
        {
            get { return _drawScale; }
            set { _drawScale = value; SetPointsFromCurve(); }
        }

        public CurveLine(IStorableAnimationCurve storable, CurveLineSettings settings)
        {
            points = new List<CurveEditorPoint>();

            _storable = storable;
            this.settings = settings;

            SetPointsFromCurve();
        }

        public void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Rect viewBounds)
        {
            //TODO: support WrapMode
            //TODO: clip y

            var curvePoints = new List<Vector2>();
            var min = _drawScale.inverse.Multiply(viewBounds.min);
            var max = _drawScale.inverse.Multiply(viewBounds.max);

            var minKeyIndex = Array.FindLastIndex(curve.keys, k => k.time < min.x);
            var maxKeyIndex = Array.FindIndex(curve.keys, k => k.time > max.x);

            if (minKeyIndex < 0) minKeyIndex = 0;
            if (maxKeyIndex < 0) maxKeyIndex = curve.length - 1;
            var keyIndex = minKeyIndex;

            for (var i = 0; i < settings.curveLineEvaluateCount; i++)
            {
                var count = curvePoints.Count;
                var x = min.x + (max.x - min.x) * (i / (settings.curveLineEvaluateCount - 1f));
                var curr = _drawScale.Multiply(new Vector2(x, curve.Evaluate(x)));
                if (count > 0 && curr.x <= curvePoints.Last().x)
                    continue;

                if(keyIndex >= minKeyIndex && keyIndex <= maxKeyIndex)
                {
                    var key = curve.keys[keyIndex];
                    if (key.time < x)
                    {
                        if (float.IsInfinity(key.inTangent) && keyIndex - 1 >= 0)
                        {
                            var prev = curve.keys[keyIndex - 1];
                            curvePoints.Add(_drawScale.Multiply(new Vector2(key.time, prev.value)));
                        }

                        curvePoints.Add(_drawScale.Multiply(new Vector2(key.time, key.value)));
                        curvePoints.Add(curr);

                        if (float.IsInfinity(key.outTangent) && keyIndex + 1 < curve.length)
                        {
                            var next = curve.keys[keyIndex + 1];
                            var nextTime = Mathf.Min(max.x, next.time);
                            curvePoints.Add(_drawScale.Multiply(new Vector2(nextTime, key.value)));
                            curvePoints.Add(_drawScale.Multiply(new Vector2(nextTime, next.value)));
                        }

                        keyIndex++;
                        continue;
                    }
                }

                if (count < 2 || x >= max.x)
                {
                    curvePoints.Add(curr);
                    continue;
                }

                var prev0 = curvePoints[count - 2];
                var prev1 = curvePoints[count - 1];
                var prevTangent = prev1 - prev0;
                var currTangent = curr - prev1;
                var prevNormal = prevTangent.Perpendicular().normalized;
                var error = prevNormal * Vector2.Dot(prevNormal, currTangent);

                if (error.magnitude > settings.curveLinePrecision)
                    curvePoints.Add(curr);
            }

            vh.AddLine(curvePoints, settings.curveLineThickness, settings.curveLineColor, viewMatrix);

            foreach (var point in points)
                point.PopulateMesh(vh, viewMatrix, viewBounds);
        }

        public void PopulateScrubberLine(VertexHelper vh, Matrix4x4 viewMatrix, Rect viewBounds, float x)
        {
            var min = _drawScale.inverse.Multiply(viewBounds.min);
            var max = _drawScale.inverse.Multiply(viewBounds.max);
            if (x < min.x || x > max.x)
                return;

            vh.AddLine(_drawScale.Multiply(new Vector2(x, min.y)), _drawScale.Multiply(new Vector2(x, max.y)), settings.scrubberLineThickness, Color.black, viewMatrix);
        }

        public void PopulateScrubberPoints(VertexHelper vh, Matrix4x4 viewMatrix, Rect viewBounds, float x)
        {
            var min = _drawScale.inverse.Multiply(viewBounds.min);
            var max = _drawScale.inverse.Multiply(viewBounds.max);
            if (x + 2 * settings.scrubberPointRadius < min.x || x - 2 * settings.scrubberPointRadius > max.x)
                return;

            var y = curve.Evaluate(x);
            if (y + 2 * settings.scrubberPointRadius < min.y || y - 2 * settings.scrubberPointRadius > max.y)
                return;

            vh.AddCircle(_drawScale.Multiply(new Vector2(x, y)), settings.scrubberPointRadius, Color.white, viewMatrix);
        }

        public void SetCurveFromPoints()
        {
            points.Sort(new UICurveEditorPointComparer());
            while (curve.length > points.Count)
                curve.RemoveKey(0);

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                var position = _drawScale.inverse.Multiply(point.position);
                var outPosition = _drawScale.inverse.Scale(point.outHandlePosition);
                var inPosition = _drawScale.inverse.Scale(point.inHandlePosition);

                var key = new Keyframe(position.x, position.y);
                key.weightedMode = (WeightedMode)(point.inHandleMode | point.outHandleMode << 1);

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
                        var prevPosition = _drawScale.inverse.Scale(prev.position);
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
                        var nextPosition = _drawScale.inverse.Scale(next.position);
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
                CreatePoint();

            for (var i = 0; i < curve.length; i++)
            {
                var point = points[i];
                var key = curve[i];

                point.position = _drawScale.Multiply(new Vector2(key.time, key.value));
                point.handleMode = key.inTangent != key.outTangent ? 1 : 0;
                point.inHandleMode = ((int)key.weightedMode & 1) > 0 ? 1 : 0;
                point.outHandleMode = ((int)key.weightedMode & 2) > 0 ? 1 : 0;

                var outHandleNormal = _drawScale.Scale(MathUtils.VectorFromAngle(Mathf.Atan(key.outTangent)).normalized);
                if (point.outHandleMode == 1 && i < curve.length - 1)
                {
                    var x = key.outWeight * _drawScale.ratio.x * (curve[i + 1].time - key.time);
                    var y = x * (outHandleNormal.y / outHandleNormal.x);
                    var length = Mathf.Sqrt(x * x + y * y);
                    point.outHandlePosition = outHandleNormal * length;
                }
                else
                {
                    point.outHandlePosition = outHandleNormal * settings.defaultPointHandleLength;
                }

                var inHandleNormal = _drawScale.Scale(-MathUtils.VectorFromAngle(Mathf.Atan(key.inTangent)).normalized);
                if (point.inHandleMode == 1 && i > 0)
                {
                    var x = key.inWeight * _drawScale.ratio.x * (key.time - curve[i - 1].time);
                    var y = x * (inHandleNormal.y / inHandleNormal.x);
                    var length = Mathf.Sqrt(x * x + y * y);
                    point.inHandlePosition = inHandleNormal * length;
                }
                else
                {
                    point.inHandlePosition = inHandleNormal * settings.defaultPointHandleLength;
                }
            }
        }

        public CurveEditorPoint CreatePoint(Vector2 position = new Vector2())
        {
            var point = new CurveEditorPoint(this, settings)
            {
                position = position
            };

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
                _selectedPoint.showHandles = false;
                _selectedPoint = null;
            }

            if (point != null)
            {
                point.showHandles = true;
                _selectedPoint = point;
            }
        }

        public float DistanceToPoint(Vector2 point)
        {
            var localPoint = _drawScale.inverse.Multiply(point);
            var screenEval = _drawScale.Multiply(new Vector2(localPoint.x, curve.Evaluate(localPoint.x)));
            return Mathf.Abs(point.y - screenEval.y);
        }

        public Vector2 GetGridCellSize(Rect viewBouns, int cellCount)
        {
            var viewMin = _drawScale.inverse.Scale(viewBouns.min);
            var viewMax = _drawScale.inverse.Scale(viewBouns.max);

            var roughStep = (viewMax - viewMin) / (cellCount - 1);

            var stepPower = new Vector2(
                Mathf.Pow(2, -Mathf.Floor(Mathf.Log(Mathf.Abs(roughStep.x), 2))),
                Mathf.Pow(2, -Mathf.Floor(Mathf.Log(Mathf.Abs(roughStep.y), 2)))
            );

            var normalizedStep = roughStep * stepPower;
            var step = new Vector2(
                Mathf.NextPowerOfTwo(Mathf.CeilToInt(normalizedStep.x)),
                Mathf.NextPowerOfTwo(Mathf.CeilToInt(normalizedStep.y))
            );

            return _drawScale.Scale(step / stepPower);
        }

        private class UICurveEditorPointComparer : IComparer<CurveEditorPoint>
        {
            public int Compare(CurveEditorPoint a, CurveEditorPoint b)
                => Comparer<float>.Default.Compare(a.position.x, b.position.x);
        }
    }
}

