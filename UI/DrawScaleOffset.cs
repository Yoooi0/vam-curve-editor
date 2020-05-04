using UnityEngine;

namespace CurveEditor.UI
{
    public class DrawScaleOffset
    {
        public DrawScaleOffset() { }
        public DrawScaleOffset(DrawScaleOffset drawScale) : this(drawScale.ratio, drawScale.offset) { }

        public DrawScaleOffset(Vector2 ratio, Vector2 offset)
        {
            this.ratio = ratio;
            this.offset = offset;
        }

        public Vector2 ratio { get; set; } = new Vector2(1f, 1f);
        public Vector2 offset { get; set; } = new Vector2(0, 0);
        public DrawScaleOffset inverse => DrawScaleOffset.Inverse(this);

        public static DrawScaleOffset FromSizeOffset(Vector2 size, Vector2 offset = new Vector2())
        {
            var ratioX = size.x < 0.0001f ? 1 : 1f / size.x;
            var ratioY = size.y < 0.0001f ? 1 : 1f / size.y;

            return new DrawScaleOffset(new Vector2(ratioX, ratioY), offset);
        }

        public static DrawScaleOffset FromNormalizedSizeOffset(Vector2 size, Vector2 unitSize, Vector2 offset = new Vector2())
        {
            var ratioX = size.x < 0.0001f ? 1 : unitSize.x / size.x;
            var ratioY = size.y < 0.0001f ? 1 : unitSize.y / size.y;

            return new DrawScaleOffset(new Vector2(ratioX, ratioY), offset);
        }

        public static DrawScaleOffset Inverse(DrawScaleOffset drawScale)
            => new DrawScaleOffset(new Vector2(1 / drawScale.ratio.x, 1 / drawScale.ratio.y), -drawScale.offset * drawScale.ratio);

        public Vector2 Scale(Vector2 value) => value * ratio;
        public Vector2 Translate(Vector2 value) => value + offset * ratio;
        public Vector2 Multiply(Vector2 value) => (value + offset) * ratio;
    }
}
