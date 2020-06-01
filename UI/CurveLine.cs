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
        private Matrix2x3 _drawMatrix = Matrix2x3.identity;

        public readonly List<CurveEditorPoint> points;

        public CurveLineSettings settings { get; }
        public AnimationCurve curve => _storable.val;

        public Matrix2x3 drawMatrix
        {
            get { return _drawMatrix; }
            set { _drawMatrix = value; SetPointsFromCurve(); }
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
            //TODO: fix moving curvePoints
            //TODO: add cliprect setting

            var curvePoints = new List<Vector2>();
            var min = _drawMatrix.inverse * viewBounds.min - Vector2.one * settings.curveLineThickness;
            var max = _drawMatrix.inverse * viewBounds.max + Vector2.one * settings.curveLineThickness;

            var minKeyIndex = Array.FindLastIndex(curve.keys, k => k.time < min.x);
            var maxKeyIndex = Array.FindIndex(curve.keys, k => k.time > max.x);

            if (minKeyIndex < 0) minKeyIndex = 0;
            if (maxKeyIndex < 0) maxKeyIndex = curve.length - 1;
            var keyIndex = minKeyIndex;

            for (var i = 0; i < settings.curveLineEvaluateCount; i++)
            {
                var x = Mathf.Lerp(min.x, max.x, i / (settings.curveLineEvaluateCount - 1f));
                var curr = _drawMatrix * new Vector2(x, curve.Evaluate(x));

                var count = curvePoints.Count;
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
                            curvePoints.Add(_drawMatrix * new Vector2(key.time, prev.value));
                        }

                        var keyPosition = _drawMatrix * new Vector2(key.time, key.value);
                        curvePoints.Add(keyPosition);

                        if(Vector2.Distance(keyPosition, curr) > 0.0001f)
                            curvePoints.Add(curr);

                        if (float.IsInfinity(key.outTangent) && keyIndex + 1 < curve.length)
                        {
                            var next = curve.keys[keyIndex + 1];
                            var nextTime = Mathf.Min(max.x, next.time);
                            curvePoints.Add(_drawMatrix * new Vector2(nextTime, key.value));
                            curvePoints.Add(_drawMatrix * new Vector2(nextTime, next.value));
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

        public void SetCurveFromPoints()
        {
            points.Sort(new UICurveEditorPointComparer());
            while (curve.length > points.Count)
                curve.RemoveKey(0);

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                var position = _drawMatrix.inverse.Multiply(point.position);
                var outPosition = _drawMatrix.inverse.Scale(point.outHandlePosition);
                var inPosition = _drawMatrix.inverse.Scale(point.inHandlePosition);

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
                        var prevPosition = _drawMatrix.inverse.Scale(prev.position);
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
                        var nextPosition = _drawMatrix.inverse.Scale(next.position);
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

                point.position = _drawMatrix * new Vector2(key.time, key.value);
                point.handleMode = key.inTangent != key.outTangent ? 1 : 0;
                point.inHandleMode = ((int)key.weightedMode & 1) > 0 ? 1 : 0;
                point.outHandleMode = ((int)key.weightedMode & 2) > 0 ? 1 : 0;

                var outHandleNormal = _drawMatrix.Scale(MathUtils.VectorFromAngle(Mathf.Atan(key.outTangent)).normalized);
                if (point.outHandleMode == 1 && i < curve.length - 1)
                {
                    var x = key.outWeight * _drawMatrix.scale.x * (curve[i + 1].time - key.time);
                    var y = x * (outHandleNormal.y / outHandleNormal.x);
                    var length = Mathf.Sqrt(x * x + y * y);
                    point.outHandlePosition = outHandleNormal * length;
                }
                else
                {
                    point.outHandlePosition = outHandleNormal * settings.defaultPointHandleLength;
                }

                var inHandleNormal = _drawMatrix.Scale(-MathUtils.VectorFromAngle(Mathf.Atan(key.inTangent)).normalized);
                if (point.inHandleMode == 1 && i > 0)
                {
                    var x = key.inWeight * _drawMatrix.scale.x * (key.time - curve[i - 1].time);
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
            var localPoint = _drawMatrix.inverse * point;
            var screenEval = _drawMatrix * new Vector2(localPoint.x, curve.Evaluate(localPoint.x));
            return Mathf.Abs(point.y - screenEval.y);
        }

        public Vector2 GetGridCellSize(Rect viewBouns, int cellCount)
        {
            var viewMin = _drawMatrix.inverse.Scale(viewBouns.min);
            var viewMax = _drawMatrix.inverse.Scale(viewBouns.max);

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

            return _drawMatrix.Scale(step / stepPower);
        }

        private class UICurveEditorPointComparer : IComparer<CurveEditorPoint>
        {
            public int Compare(CurveEditorPoint a, CurveEditorPoint b)
                => Comparer<float>.Default.Compare(a.position.x, b.position.x);
        }
    }
}

