using UnityEngine;

namespace CurveEditor.UI
{
    public class DrawScaleOffset
    {
        public DrawScaleOffset() { }
        public DrawScaleOffset(DrawScaleOffset drawScale) : this(drawScale.offset, drawScale.ratio) { }

        public DrawScaleOffset(Vector2 offset, Vector2 ratio)
        {
            this.offset = offset;
            this.ratio = ratio;
        }

        public Vector2 offset { get; private set; } = new Vector2(0, 0);
        public Vector2 ratio { get; private set; } = new Vector2(1f, 1f);
        public DrawScaleOffset inverse => DrawScaleOffset.Inverse(this);

        public static DrawScaleOffset FromValueBounds(Rect valueBounds, Vector2 offset = new Vector2())
        {
            var ratioX = valueBounds.size.x < 0.0001f ? 1 : 1f / valueBounds.size.x;
            var ratioY = valueBounds.size.y < 0.0001f ? 1 : 1f / valueBounds.size.y;

            return new DrawScaleOffset(offset, new Vector2(ratioX, ratioY));
        }

        public static DrawScaleOffset FromNormalizedValueBounds(Rect valueBounds, Vector2 unitSize, Vector2 offset = new Vector2())
        {
            var ratioX = valueBounds.size.x < 0.0001f ? 1 : unitSize.x / valueBounds.size.x;
            var ratioY = valueBounds.size.y < 0.0001f ? 1 : unitSize.y / valueBounds.size.y;

            return new DrawScaleOffset(offset, new Vector2(ratioX, ratioY));
        }

        public static DrawScaleOffset Inverse(DrawScaleOffset drawScale) // TODO: probably wrong 
            => new DrawScaleOffset(-drawScale.offset * drawScale.ratio, new Vector2(1 / drawScale.ratio.x, 1 / drawScale.ratio.y));

        public void Resize(float v) => ratio *= v;
        public Vector2 Scale(Vector2 value) => value * ratio;
        public Vector2 Translate(Vector2 value) => value + offset;
        public Vector2 Multiply(Vector2 value) => (value + offset) * ratio;
    }
}
