using UnityEngine;

namespace CurveEditor.UI
{
    public class DrawScaleOffset
    {
        private DrawScaleOffset drawScale;

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

        public static DrawScaleOffset FromValueBounds(Rect valueBounds)
        {
            Vector2 offset, ratio;
            offset.x = valueBounds.size.x < 0.0001f ? 0 : valueBounds.min.x;
            offset.y = valueBounds.size.y < 0.0001f ? 0 : valueBounds.min.y;
            ratio.x = valueBounds.size.x < 0.0001f ? 1 : 1f / valueBounds.size.x;
            ratio.y = valueBounds.size.y < 0.0001f ? 1 : 1f / valueBounds.size.y;

            return new DrawScaleOffset(offset, ratio);
        }

        public static DrawScaleOffset FromViewNormalizedValueBounds(Rect valueBounds, Vector2 viewSize)
        {
            Vector2 offset, ratio;
            offset.x = valueBounds.size.x < 0.0001f ? 0 : valueBounds.min.x * viewSize.x;
            offset.y = valueBounds.size.y < 0.0001f ? 0 : valueBounds.min.y * viewSize.y;
            ratio.x = valueBounds.size.x < 0.0001f ? 1 : viewSize.x / valueBounds.size.x;
            ratio.y = valueBounds.size.y < 0.0001f ? 1 : viewSize.y / valueBounds.size.y;

            return new DrawScaleOffset(offset, ratio);
        }

        public static DrawScaleOffset Inverse(DrawScaleOffset drawScale) // TODO: probably wrong 
            => new DrawScaleOffset(-drawScale.offset * drawScale.ratio, new Vector2(1 / drawScale.ratio.x, 1 / drawScale.ratio.y));

        public void Resize(float v) => ratio *= v;
        public Vector2 Scale(Vector2 value) => value * ratio;
        public Vector2 Translate(Vector2 value) => value + offset;
        public Vector2 Multiply(Vector2 value) => (value + offset) * ratio;
    }
}
