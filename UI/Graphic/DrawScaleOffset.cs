using UnityEngine;

namespace CurveEditor.UI
{
    public struct DrawScaleOffset
    {
        public Bounds valueBounds;
        public Bounds viewBounds;
        public Vector2 offset;
        public Vector2 ratio;

        public static DrawScaleOffset FromBounds(Bounds viewBounds, Bounds valueBounds)
        {
            DrawScaleOffset value;
            value.viewBounds = viewBounds;
            value.valueBounds = valueBounds;
            value.offset = new Vector2(valueBounds.min.x * viewBounds.size.x, valueBounds.min.y * viewBounds.size.y);
            value.ratio = new Vector2(1f / valueBounds.size.x * viewBounds.size.x, 1f / valueBounds.size.y * viewBounds.size.y);
            return value;
        }

        public Vector2 Apply(Vector2 value)
        {
            return value * ratio + offset;
        }

        public Vector2 Reverse(Vector2 value)
        {
            return (value - offset) / ratio;
        }
    }
}
