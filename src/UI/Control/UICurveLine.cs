using System.Collections.Generic;
using CurveEditor.Utils;
using UnityEngine;

namespace CurveEditor.UI
{
    public class UICurveLine
    {
        public UILine line { get; set; }
        public AnimationCurve curve { get; set; }
        public List<UICurveEditorPoint> points { get; } = new List<UICurveEditorPoint>();
        private readonly UIColors _colors;
        private int _evaluateCount = 200;

        public int evaluateCount
        {
            get { return _evaluateCount; }
            set { _evaluateCount = value; UpdateCurve(); }
        }

        public UICurveLine(AnimationCurve curve, UILine line, UIColors colors)
        {
            this.curve = curve;
            this.line = line;
            _colors = colors;
        }

        public void UpdateCurve()
        {
            var sizeDelta = line.rectTransform.sizeDelta;

            points.Sort(new UICurveEditorPointComparer());
            while (curve.keys.Length > points.Count)
                curve.RemoveKey(0);

            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];

                var position = point.rectTransform.anchoredPosition / sizeDelta;

                var key = new Keyframe(position.x, position.y);
                key.weightedMode = (WeightedMode)(point.inHandleMode | point.outHandleMode << 1);

                var outPosition = point.outHandlePosition / sizeDelta;
                var inPosition = point.inHandlePosition / sizeDelta;

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
                        var prevPosition = prev.rectTransform.anchoredPosition / sizeDelta;
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
                        var nextPosition = next.rectTransform.anchoredPosition / sizeDelta;
                        var dx = nextPosition.x - position.x;
                        key.outWeight = Mathf.Clamp(Mathf.Abs(outPosition.x / dx), 0f, 1f);
                    }
                }

                if (i >= curve.keys.Length)
                    curve.AddKey(key);
                else
                    curve.MoveKey(i, key);
            }

            var result = new List<Vector2>();
            for (var i = 0; i < _evaluateCount; i++)
            {
                var t = (float)i / (_evaluateCount - 1);
                var value = curve.Evaluate(t);
                result.Add(new Vector2(t * sizeDelta.x, value * sizeDelta.y));
            }

            line.points = result;
        }

        public IList<UICurveEditorPoint> SetPointsFromKeyframes(List<Keyframe> keyframes)
        {
            var sizeDelta = line.rectTransform.sizeDelta;

            while (points.Count > keyframes.Count)
                DestroyPoint(points[0]);
            while (points.Count < keyframes.Count)
                CreatePoint(new Vector2());

            for (var i = 0; i < keyframes.Count; i++)
            {
                var point = points[i];
                var key = keyframes[i];
                point.rectTransform.anchoredPosition = new Vector2(key.time, key.value) * sizeDelta;

                if (key.inTangent != key.outTangent)
                    point.handleMode = 1;

                if (((int)key.weightedMode & 1) > 0) point.inHandleMode = 1;
                if (((int)key.weightedMode & 2) > 0) point.outHandleMode = 1;

                var outHandleNormal = (MathUtils.VectorFromAngle(Mathf.Atan(key.outTangent)) * sizeDelta).normalized;
                if (point.outHandleMode == 1 && i < keyframes.Count - 1)
                {
                    var x = key.outWeight * (keyframes[i + 1].time - key.time) * sizeDelta.x;
                    var y = x * (outHandleNormal.y / outHandleNormal.x);
                    var length = Mathf.Sqrt(x * x + y * y);
                    point.outHandlePosition = outHandleNormal * length;
                }
                else
                {
                    point.outHandlePosition = outHandleNormal * point.outHandleLength;
                }

                var inHandleNormal = -(MathUtils.VectorFromAngle(Mathf.Atan(key.inTangent)) * sizeDelta).normalized;
                if (point.inHandleMode == 1 && i > 0)
                {
                    var x = key.inWeight * (key.time - keyframes[i - 1].time) * sizeDelta.x;
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

            UpdateCurve();

            return points;
        }

        public UICurveEditorPoint CreatePoint(Vector2 position)
        {
            var pointObject = new GameObject();
            pointObject.transform.SetParent(line.transform, false);

            var point = pointObject.AddComponent<UICurveEditorPoint>();
            point.owner = this;
            point.draggingRect = line.rectTransform;
            point.color = _colors.pointColor;
            point.inHandleColor = _colors.inHandleColor;
            point.outHandleColor = _colors.outHandleColor;
            point.lineColor = _colors.handleLineColor;

            point.rectTransform.anchoredPosition = position;

            points.Add(point);

            return point;
        }

        public void DestroyPoint(UICurveEditorPoint point)
        {
            points.Remove(point);
            UnityEngine.Object.Destroy(point.gameObject);
            UpdateCurve();
        }

        public void SetHandleMode(UICurveEditorPoint point, int mode)
        {
            point.handleMode = mode;
            point.lineColor = mode == 0 ? _colors.handleLineColor : _colors.handleLineColorFree;
        }

        public void SetOutHandleMode(UICurveEditorPoint point, int mode)
        {
            point.outHandleMode = mode;
            point.outHandleColor = mode == 0 ? _colors.outHandleColor : _colors.outHandleColorWeighted;
        }

        public void SetInHandleMode(UICurveEditorPoint point, int mode)
        {
            point.inHandleMode = mode;
            point.inHandleColor = mode == 0 ? _colors.inHandleColor : _colors.inHandleColorWeighted;
        }

        private class UICurveEditorPointComparer : IComparer<UICurveEditorPoint>
        {
            public int Compare(UICurveEditorPoint x, UICurveEditorPoint y)
                => Comparer<float>.Default.Compare(x.rectTransform.anchoredPosition.x, y.rectTransform.anchoredPosition.x);
        }
    }
}

