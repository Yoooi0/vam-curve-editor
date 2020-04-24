using Leap.Unity;
using Leap.Unity.Swizzle;
using System;
using UnityEngine;
using VikingCrewTools.UI;

namespace CurveEditor.UI
{
    public class DrawScaleOffset
    {
        public Vector2 offset { get; private set; } = new Vector2(0, 0);
        public Vector2 ratio { get; private set; } = new Vector2(1f, 1f);
        public DrawScaleOffset inverse => DrawScaleOffset.Inverse(this);

        public static DrawScaleOffset FromValueBounds(Bounds valueBounds)
        {
            Vector2 offset, ratio;
            offset.x = valueBounds.size.x < 0.0001f ? 0 : valueBounds.min.x;
            offset.y = valueBounds.size.y < 0.0001f ? 0 : valueBounds.min.y;
            ratio.x = valueBounds.size.x < 0.0001f ? 1 : 1f / valueBounds.size.x;
            ratio.y = valueBounds.size.y < 0.0001f ? 1 : 1f / valueBounds.size.y;

            return FromOffsetRatio(offset, ratio);
        }

        public static DrawScaleOffset FromViewNormalizedValueBounds(Bounds valueBounds, Bounds viewBounds)
        {
            Vector2 offset, ratio;
            offset.x = valueBounds.size.x < 0.0001f ? 0 : valueBounds.min.x * viewBounds.size.x;
            offset.y = valueBounds.size.y < 0.0001f ? 0 : valueBounds.min.y * viewBounds.size.y;
            ratio.x = valueBounds.size.x < 0.0001f ? 1 : viewBounds.size.x / valueBounds.size.x;
            ratio.y = valueBounds.size.y < 0.0001f ? 1 : viewBounds.size.y / valueBounds.size.y;

            return FromOffsetRatio(offset, ratio);
        }

        public static DrawScaleOffset Inverse(DrawScaleOffset drawScale) // TODO: probably wrong 
            => FromOffsetRatio(-drawScale.offset * drawScale.ratio, new Vector2(1 / drawScale.ratio.x, 1 / drawScale.ratio.y));
        public static DrawScaleOffset FromOffsetRatio(Vector2 offset, Vector2 ratio)
            => new DrawScaleOffset() { offset = offset, ratio = ratio };

        public void Resize(float v) => ratio *= v;
        public Vector2 Scale(Vector2 value) => value * ratio;
        public Vector2 Translate(Vector2 value) => value + offset;
        public Vector2 Multiply(Vector2 value) => (value + offset) * ratio;
    }
}
