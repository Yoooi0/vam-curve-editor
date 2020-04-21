using Leap.Unity;
using System;
using UnityEngine;

namespace CurveEditor.UI
{
    public class DrawScaleOffset
    {
        public Vector2 offset { get; private set; } = new Vector2(0, 0);
        public Vector2 ratio { get; private set; } = new Vector2(1f, 1f);

        public static DrawScaleOffset FromValueBounds(Bounds valueBounds)
        {
            Vector2 offset, ratio;
            offset.x = valueBounds.size.x < 0.0001f ? 0 : valueBounds.min.x / valueBounds.size.x;
            offset.y = valueBounds.size.y < 0.0001f ? 0 : valueBounds.min.y / valueBounds.size.y;
            ratio.x = valueBounds.size.x < 0.0001f ? 1 : 1f / valueBounds.size.x;
            ratio.y = valueBounds.size.y < 0.0001f ? 1 : 1f / valueBounds.size.y;

            return FromOffsetRatio(-offset, ratio);
        }

        public static DrawScaleOffset FromViewNormalizedValueBounds(Bounds valueBounds, Bounds viewBounds)
        {
            Vector2 offset, ratio;
            offset.x = valueBounds.size.x < 0.0001f ? 0 : valueBounds.min.x * viewBounds.size.x / valueBounds.size.x;
            offset.y = valueBounds.size.y < 0.0001f ? 0 : valueBounds.min.y * viewBounds.size.y / valueBounds.size.y;
            ratio.x = valueBounds.size.x < 0.0001f ? 1 : viewBounds.size.x / valueBounds.size.x;
            ratio.y = valueBounds.size.y < 0.0001f ? 1 : viewBounds.size.y / valueBounds.size.y;

            return FromOffsetRatio(-offset, ratio);
        }

        public static DrawScaleOffset FromOffsetRatio(Vector2 offset, Vector2 ratio)
            => new DrawScaleOffset() { offset = offset, ratio = ratio };

        public void Resize(float v) => ratio *= v;
        public Vector2 Apply(Vector2 value) => value * ratio + offset;
        public Vector2 Reverse(Vector2 value) => (value - offset) / ratio;
        public float ApplyX(float x) => x * ratio.x / offset.x;
        public float ApplyY(float y) => y * ratio.y / offset.y;
        public float ReverseX(float x) => (x - offset.x) / ratio.x;
        public float ReverseY(float y) => (y - offset.y) / ratio.y;
    }
}
