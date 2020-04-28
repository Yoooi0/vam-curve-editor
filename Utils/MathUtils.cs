using Leap.Unity.Swizzle;
using UnityEngine;

namespace CurveEditor.Utils
{
    public static class MathUtils
    {
        public static Vector2 VectorFromAngle(float angle)
            => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        public static float DistanceToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            var diff = b - a;
            var dist = diff.magnitude;
            if (dist < 0.00001f)
                return Vector2.Distance(p, a);

            var t = Mathf.Clamp(Vector2.Dot(p - a, diff.normalized), 0, dist);
            return Vector2.Distance(p, a + diff.normalized * t);
        }

        public static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
            => Quaternion.Euler(angles) * (point - pivot) + pivot;
        public static Vector2 RotatePointAroundPivot(Vector2 point, Vector2 pivot, float angle)
            => RotatePointAroundPivot(point, pivot, angle * Vector3.forward);

        public static Vector2 MultiplyPoint2d(this Matrix4x4 m, Vector2 point) => m.MultiplyPoint3x4(point).xy();
        public static Vector2 MultiplyPoint2d(this Matrix4x4 m, Vector3 point) => m.MultiplyPoint3x4(point).xy();

        public static Rect CenterSizeRect(Vector2 center, Vector2 size)
            => new Rect(center - size / 2, size);

        public static Rect Encapsulate(this Rect rect, Rect other)
        {
            var min = Vector2.Min(rect.min, other.min);
            var max = Vector2.Max(rect.max, other.max);

            return new Rect(min, max - min);
        }
    }
}
