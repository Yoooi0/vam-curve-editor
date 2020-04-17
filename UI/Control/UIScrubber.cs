using CurveEditor.Utils;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    //TODO: cant have different line/point color because with multiple curves there are z-order issues
    public class UIScrubber : MaskableGraphic
    {
        private float _lineThickness = 4;
        private float _pointRadius = 6;
        private bool _showPoint = true;
        private bool _showLine = false;

        public float lineThickness
        {
            get { return _lineThickness; }
            set { _lineThickness = value; SetVerticesDirty(); }
        }

        public float pointRadius
        {
            get { return _pointRadius; }
            set { _pointRadius = value; SetVerticesDirty(); }
        }

        public bool showPoint
        {
            get { return _showPoint; }
            set { _showPoint = value; SetVerticesDirty(); }
        }

        public bool showLine
        {
            get { return _showLine; }
            set { _showLine = value; SetVerticesDirty(); }
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

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if(_showLine)
                DrawLine(vh, new Vector2(0, -0.5f) * rectTransform.sizeDelta, new Vector2(0, 0.5f) * rectTransform.sizeDelta, _lineThickness, color);

            if (_showPoint)
                DrawDot(vh, Vector2.zero, _pointRadius, color);
        }

        private void DrawDot(VertexHelper vh, Vector2 position, float radius, Color color)
        {
            const int segments = 10;
            var prev = position;
            for (var i = 0; i < segments + 1; i++)
            {
                var rad = Mathf.Deg2Rad * (i * (360f / segments));
                var pos0 = prev;
                var pos1 = position + new Vector2(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad));
                prev = pos1;
                vh.AddUIVertexQuad(CreateVbo(new[] { pos0, pos1, position, position }, color));
            }
        }

        private void DrawLine(VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var prev = from;
            var cur = to;
            var angle = Mathf.Atan2(cur.y - prev.y, cur.x - prev.x) * 180f / Mathf.PI;

            var v1 = prev + new Vector2(0, -thickness / 2);
            var v2 = prev + new Vector2(0, +thickness / 2);
            var v3 = cur + new Vector2(0, +thickness / 2);
            var v4 = cur + new Vector2(0, -thickness / 2);

            v1 = MathUtils.RotatePointAroundPivot(v1, prev, angle);
            v2 = MathUtils.RotatePointAroundPivot(v2, prev, angle);
            v3 = MathUtils.RotatePointAroundPivot(v3, cur, angle);
            v4 = MathUtils.RotatePointAroundPivot(v4, cur, angle);

            vh.AddUIVertexQuad(CreateVbo(new[] { v1, v2, v3, v4 }, color));
        }
    }
}
