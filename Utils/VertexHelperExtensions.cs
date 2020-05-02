using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.Utils
{
    public static class VertexHelperExtensions
    {
        private static UIVertex[] CreateVBO(Color color, Matrix4x4 viewMatrix, params Vector2[] vertices)
        {
            var vbo = new UIVertex[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = viewMatrix.MultiplyPoint2d(vertices[i]);
                vbo[i] = vert;
            }

            return vbo;
        }

        public static void AddUIVertexQuad(this VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color, Matrix4x4 viewMatrix)
            => vh.AddUIVertexQuad(CreateVBO(color, viewMatrix, a, b, c, d));

        public static void AddUIVertexTriangle(this VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color, Matrix4x4 viewMatrix)
        {
            var count = vh.currentVertCount;
            foreach (var v in CreateVBO(color, viewMatrix, a, b, c))
                vh.AddVert(v);

            vh.AddTriangle(count, count + 1, count + 2);
        }

        public static void AddCircle(this VertexHelper vh, Vector2 position, float radius, Color color, Matrix4x4 viewMatrix, int segments = 6, float rotation = 0)
        {
            if (segments < 3)
                return;

            var vo = position + MathUtils.VectorFromAngle(Mathf.Deg2Rad * rotation) * radius;
            for (int i = 1, j = 2; j < segments; i = j++)
            {
                var vi = position + MathUtils.VectorFromAngle(Mathf.Deg2Rad * (rotation + i * (360f / segments))) * radius;
                var vj = position + MathUtils.VectorFromAngle(Mathf.Deg2Rad * (rotation + j * (360f / segments))) * radius;
                vh.AddUIVertexTriangle(vo, vi, vj, color, viewMatrix);
            }
        }

        public static void AddSquare(this VertexHelper vh, Vector2 position, float size, Color color, Matrix4x4 viewMatrix)
            => vh.AddCircle(position, size * Mathf.Sqrt(2), color, viewMatrix, 4, 45);

        public static void AddLine(this VertexHelper vh, Vector2 from, Vector2 to, float thickness, Color color, Matrix4x4 viewMatrix)
            => vh.AddLine(thickness, color, viewMatrix, from, to);
        public static void AddLine(this VertexHelper vh, float thickness, Color color, Matrix4x4 viewMatrix, params Vector2[] points)
            => vh.AddLine(points, thickness, color, viewMatrix);

        public static void AddLine(this VertexHelper vh, IList<Vector2> points, float thickness, Color color, Matrix4x4 viewMatrix)
        {
            if (points == null || points.Count < 2)
                return;

            for (var i = 1; i < points.Count; i++)
            {
                var prev = points[i - 1];
                var curr = points[i];
                var angle = Mathf.Atan2(curr.y - prev.y, curr.x - prev.x) * Mathf.Rad2Deg;

                var v1 = prev + new Vector2(0, -thickness / 2);
                var v2 = prev + new Vector2(0, +thickness / 2);
                var v3 = curr + new Vector2(0, +thickness / 2);
                var v4 = curr + new Vector2(0, -thickness / 2);

                v1 = MathUtils.RotatePointAroundPivot(v1, prev, angle);
                v2 = MathUtils.RotatePointAroundPivot(v2, prev, angle);
                v3 = MathUtils.RotatePointAroundPivot(v3, curr, angle);
                v4 = MathUtils.RotatePointAroundPivot(v4, curr, angle);

                vh.AddUIVertexQuad(v1, v2, v3, v4, color, viewMatrix);
            }
        }
    }
}
