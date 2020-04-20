using UnityEngine;

namespace CurveEditor.UI
{
    public struct DrawScaleOffset
    {
        public Bounds valueBounds;
        public Vector2 offset;
        public Vector2 ratio;

        public static DrawScaleOffset FromBounds(Bounds viewBounds, Bounds valueBounds)
        {
            DrawScaleOffset value;
            value.valueBounds = valueBounds;
            value.offset = new Vector2(valueBounds.min.x * viewBounds.size.x, valueBounds.min.y * viewBounds.size.y);
            value.ratio = new Vector2(1f / valueBounds.size.x * viewBounds.size.x, 1f / valueBounds.size.y * viewBounds.size.y);
            return value;
        }
    }
}
