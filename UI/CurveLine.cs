using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public UICurveLineSettings settings { get; private set; }
        public AnimationCurve curve => _storable.val;
        
        public DrawScaleOffset drawScale
        {
            get { return _drawScale; }
            set { _drawScale = value; SetPointsFromCurve(); }
        }

        public CurveLine(IStorableAnimationCurve storable, UICurveLineSettings settings)
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

            vh.AddLine(_drawScale.Multiply(new Vector2(x, min.y)), _drawScale.Multiply(new Vector2(x, max.y)), 0.02f, Color.black, viewMatrix);
        }

        public void PopulateScrubberPoints(VertexHelper vh, Matrix4x4 viewMatrix, Rect viewBounds, float x)
        {
            var min = _drawScale.inverse.Multiply(viewBounds.min);
            var max = _drawScale.inverse.Multiply(viewBounds.max);
            if (x + 0.06f < min.x || x - 0.06f > max.x)
                return;

            var y = curve.Evaluate(x);
            if (y + 0.06f < min.y || y - 0.06f > max.y)
                return;

            vh.AddCircle(_drawScale.Multiply(new Vector2(x, y)), 0.03f, Color.white, viewMatrix);
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
                    point.outHandlePosition = outHandleNormal * point.outHandleLength;
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
                    point.inHandlePosition = inHandleNormal * point.inHandleLength;
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
            var localPoint = drawScale.inverse.Multiply(point);
            var screenEval = drawScale.Multiply(new Vector2(localPoint.x, curve.Evaluate(localPoint.x)));
            return Mathf.Abs(point.y - screenEval.y);
        }

        private class UICurveEditorPointComparer : IComparer<CurveEditorPoint>
        {
            public int Compare(CurveEditorPoint x, CurveEditorPoint y)
                => Comparer<float>.Default.Compare(x.position.x, y.position.x);
        }
    }

    public class UICurveLineSettings : INotifyPropertyChanged
    {
        private Color _pointDotColor = new Color(0.427f, 0.035f, 0.517f);
        private Color _pointDotColorSelected = new Color(0.682f, 0.211f, 0.788f);
        private Color _pointHandleLineColor = new Color(0, 0, 0);
        private Color _pointHandleLineColorFree = new Color(0.427f, 0.035f, 0.517f);
        private Color _pointHandleDotColor = new Color(0, 0, 0);
        private Color _pointHandleDotColorWeighted = new Color(0.427f, 0.035f, 0.517f);

        private float _pointDotRadius = 0.08f;
        private float _pointDotSkin = 0.04f;
        private float _pointHandleDotRadius = 0.07f;
        private float _pointHandleDotSkin = 0.04f;
        private float _pointShellSize = 0.2f;
        private float _pointHandleLineThickness = 0.03f;
        private float _defaultHandleLength = 0.5f;

        private float _curveLinePrecision = 0.01f;
        private float _curveLineThickness = 0.04f;
        private int _curveLineEvaluateCount = 100;
        private Color _curveLineColor = new Color(0.9f, 0.9f, 0.9f);
        private Color _scrubberColor = new Color(0.382f, 0.111f, 0.488f);

        protected UICurveLineSettings() { }

        public static UICurveLineSettings Default() => new UICurveLineSettings();
        public static UICurveLineSettings Colorize(Color tint, UICurveLineSettings settings = null)
        {
            //TODO: proper palette generator

            settings = settings ?? Default();

            float h, s, v;
            Color.RGBToHSV(tint, out h, out s, out v);

            var darkColor = Color.HSVToRGB(h, s, v * 0.8f);
            var veryDarkColor = Color.HSVToRGB(h, s, v * 0.5f);
            var desaturatedColor = Color.HSVToRGB(h, s * 0.5f, 1);

            settings.pointDotColor = darkColor;
            settings.pointDotColorSelected = tint;
            settings.pointHandleLineColor = veryDarkColor;
            settings.pointHandleLineColorFree = darkColor;
            settings.pointHandleDotColor = veryDarkColor;
            settings.pointHandleDotColorWeighted = darkColor;
            settings.curveLineColor = desaturatedColor;
            settings.scrubberColor = Color.HSVToRGB(h, s * 1.2f, v * 0.9f);

            return settings;
        }

        #region INotifyPropertyChanged
        public Color pointDotColor { get { return _pointDotColor; } set { Set(ref _pointDotColor, value, nameof(pointDotColor)); } }
        public Color pointDotColorSelected { get { return _pointDotColorSelected; } set { Set(ref _pointDotColorSelected, value, nameof(pointDotColorSelected)); } }
        public Color pointHandleLineColor { get { return _pointHandleLineColor; } set { Set(ref _pointHandleLineColor, value, nameof(pointHandleLineColor)); } }
        public Color pointHandleLineColorFree { get { return _pointHandleLineColorFree; } set { Set(ref _pointHandleLineColorFree, value, nameof(pointHandleLineColorFree)); } }
        public Color pointHandleDotColor { get { return _pointHandleDotColor; } set { Set(ref _pointHandleDotColor, value, nameof(pointHandleDotColor)); } }
        public Color pointHandleDotColorWeighted { get { return _pointHandleDotColorWeighted; } set { Set(ref _pointHandleDotColorWeighted, value, nameof(pointHandleDotColorWeighted)); } }

        public float pointDotRadius { get { return _pointDotRadius; } set { Set(ref _pointDotRadius, value, nameof(pointDotRadius)); } }
        public float pointDotSkin { get { return _pointDotSkin; } set { Set(ref _pointDotSkin, value, nameof(pointDotSkin)); } }
        public float pointHandleDotRadius { get { return _pointHandleDotRadius; } set { Set(ref _pointHandleDotRadius, value, nameof(pointHandleDotRadius)); } }
        public float pointHandleDotSkin { get { return _pointHandleDotSkin; } set { Set(ref _pointHandleDotSkin, value, nameof(pointHandleDotSkin)); } }
        public float pointShellSize { get { return _pointShellSize; } set { Set(ref _pointShellSize, value, nameof(pointShellSize)); } }
        public float pointHandleLineThickness { get { return _pointHandleLineThickness; } set { Set(ref _pointHandleLineThickness, value, nameof(pointHandleLineThickness)); } }
        public float defaultHandleLength { get { return _defaultHandleLength; } set { Set(ref _defaultHandleLength, value, nameof(defaultHandleLength)); } }

        public float curveLinePrecision { get { return _curveLinePrecision; } set { Set(ref _curveLinePrecision, value, nameof(curveLinePrecision)); } }
        public float curveLineThickness { get { return _curveLineThickness; } set { Set(ref _curveLineThickness, value, nameof(curveLineThickness)); } }
        public int curveLineEvaluateCount { get { return _curveLineEvaluateCount; } set { Set(ref _curveLineEvaluateCount, value, nameof(curveLineEvaluateCount)); } }
        public Color curveLineColor { get { return _curveLineColor; } set { Set(ref _curveLineColor, value, nameof(curveLineColor)); } }
        public Color scrubberColor { get { return _scrubberColor; } set { Set(ref _scrubberColor, value, nameof(scrubberColor)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool Set<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}

