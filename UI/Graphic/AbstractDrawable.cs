using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public abstract class AbstractDrawable
    {
        public abstract void PopulateMesh(VertexHelper vh, Matrix4x4 viewMatrix, Bounds viewBounds);
    }
}
