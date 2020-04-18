using CurveEditor.Utils;
using Leap.Unity.Swizzle;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public static class VertexHelperExtensions
    {
        public static void AddUIVertexQuad(this VertexHelper vh, Vector2[] vertices, Color color, Matrix4x4 viewMatrix)
        {
            var vbo = new UIVertex[4];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = viewMatrix.MultiplyPoint3x4(vertices[i]).xy();
                vbo[i] = vert;
            }

            vh.AddUIVertexQuad(vbo);
        }

        public static void DrawCircle(this VertexHelper vh, Vector2 position, float radius, Color color, Matrix4x4 viewMatrix)
        {
            const int segments = 10;

            var prev = position;
            for (var i = 0; i < segments + 1; i++)
            {
                var rad = Mathf.Deg2Rad * (i * (360f / segments));
                var pos0 = prev;
                var pos1 = position + new Vector2(radius * Mathf.Cos(rad), radius * Mathf.Sin(rad));
                prev = pos1;
                vh.AddUIVertexQuad(new[] { pos0, pos1, position, position }, color, viewMatrix);
            }
        }

        public static void DrawLine(this VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color, Matrix4x4 viewMatrix)
            => DrawLine(vh, new List<Vector2> { from, to }, thickness, color, viewMatrix);

        public static void DrawLine(this VertexHelper vh, List<Vector2> points, float thickness, Color color, Matrix4x4 viewMatrix)
        {
            if (points == null || points.Count < 2)
                return;

            for (var i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var angle = Mathf.Atan2(curr.y - prev.y, curr.x - prev.x) * 180f / Mathf.PI;

                var v1 = prev + new Vector2(0, -thickness / 2);
                var v2 = prev + new Vector2(0, +thickness / 2);
                var v3 = curr + new Vector2(0, +thickness / 2);
                var v4 = curr + new Vector2(0, -thickness / 2);

                v1 = MathUtils.RotatePointAroundPivot(v1, prev, angle);
                v2 = MathUtils.RotatePointAroundPivot(v2, prev, angle);
                v3 = MathUtils.RotatePointAroundPivot(v3, curr, angle);
                v4 = MathUtils.RotatePointAroundPivot(v4, curr, angle);

                vh.AddUIVertexQuad(new[] { v1, v2, v3, v4 }, color, viewMatrix);
            }
        }
    }
}
