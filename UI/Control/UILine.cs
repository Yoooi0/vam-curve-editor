using CurveEditor.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class UILine : MaskableGraphic
    {
        private List<Vector2> _points;
        private float _lineThickness = 2;

        public float lineThickness
        {
            get { return _lineThickness; }
            set { _lineThickness = value; SetVerticesDirty(); }
        }

        public List<Vector2> points
        {
            get { return _points; }
            set { _points = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (_points == null || _points.Count < 2)
                return;

            vh.Clear();
            var sizeX = 1;// rectTransform.rect.width;
            var sizeY = 1;// rectTransform.rect.height;
            var offsetX = -rectTransform.pivot.x * rectTransform.rect.width;
            var offsetY = -rectTransform.pivot.y * rectTransform.rect.height;

            var prevV1 = Vector2.zero;
            var prevV2 = Vector2.zero;

            for (var i = 1; i < _points.Count; i++)
            {
                var prev = _points[i - 1];
                var cur = _points[i];
                prev = new Vector2(prev.x * sizeX + offsetX, prev.y * sizeY + offsetY);
                cur = new Vector2(cur.x * sizeX + offsetX, cur.y * sizeY + offsetY);

                var angle = Mathf.Atan2(cur.y - prev.y, cur.x - prev.x) * 180f / Mathf.PI;

                var v1 = prev + new Vector2(0, -lineThickness / 2);
                var v2 = prev + new Vector2(0, +lineThickness / 2);
                var v3 = cur + new Vector2(0, +lineThickness / 2);
                var v4 = cur + new Vector2(0, -lineThickness / 2);

                v1 = MathUtils.RotatePointAroundPivot(v1, prev, angle);
                v2 = MathUtils.RotatePointAroundPivot(v2, prev, angle);
                v3 = MathUtils.RotatePointAroundPivot(v3, cur, angle);
                v4 = MathUtils.RotatePointAroundPivot(v4, cur, angle);

                if (i > 1)
                    CreateVbo(new[] { prevV1, prevV2, v1, v2 });

                vh.AddUIVertexQuad(CreateVbo(new[] { v1, v2, v3, v4 }));

                prevV1 = v3;
                prevV2 = v4;
            }
        }

        private UIVertex[] CreateVbo(Vector2[] vertices)
        {
            var VboVertices = new UIVertex[4];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = vertices[i];
                VboVertices[i] = vert;
            }
            return VboVertices;
        }
    }
}
