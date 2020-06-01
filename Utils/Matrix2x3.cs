using UnityEngine;

namespace CurveEditor.Utils
{
    public class Matrix2x3
    {
        public Vector2 translation { get; set; }
        public Vector2 scale { get; set; }
        public Matrix2x3 inverse => Matrix2x3.Inverse(this);

        public Matrix2x3(Matrix2x3 m) : this(m.translation, m.scale) { }
        public Matrix2x3(Vector2 translation, Vector2 scale)
        {
            this.translation = translation;
            this.scale = scale;
        }


        public Vector2 Scale(Vector2 value) => value * scale;
        public Vector2 Translate(Vector2 value) => value + translation;
        public Vector2 Multiply(Vector2 value) => value * scale + translation;

        public static Matrix2x3 FromTranslation(float x, float y) => new Matrix2x3(new Vector2(x, y), Vector2.one);
        public static Matrix2x3 FromTranslation(Vector2 translation) => new Matrix2x3(translation, Vector2.one);
        public static Matrix2x3 FromScale(float scale) => new Matrix2x3(Vector2.zero, Vector2.one * scale);
        public static Matrix2x3 FromScale(Vector2 scale) => new Matrix2x3(Vector2.zero, scale);
        public static Matrix2x3 FromTranslationScale(Vector2 translation, Vector2 scale) => new Matrix2x3(translation, scale);

        public static Matrix2x3 FromTranslationSize(Vector2 translation, Vector2 size) => FromNormalizedTranslationSize(translation, size, Vector2.one);
        public static Matrix2x3 FromNormalizedTranslationSize(Vector2 translation, Vector2 size, Vector2 unitSize)
        {
            var sx = (size.x < 0.0001f) ? 1 : (unitSize.x / size.x);
            var sy = (size.y < 0.0001f) ? 1 : (unitSize.y / size.y);
            var scale = new Vector2(sx, sy);

            return new Matrix2x3(translation * scale, scale);
        }

        public static Matrix2x3 identity => new Matrix2x3(Vector2.zero, Vector2.one);
        public static Matrix2x3 Inverse(Matrix2x3 m) => new Matrix2x3(-m.translation / m.scale, new Vector2(1 / m.scale.x, 1 / m.scale.y));

        public static Vector2 operator *(Matrix2x3 m, Vector2 v) => m.Multiply(v);
        public override string ToString() => $"(tx: {translation.x}, ty: {translation.y}, sx: {scale.x}, sy: {scale.y})";
    }
}
