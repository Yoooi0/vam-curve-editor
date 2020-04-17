using UnityEngine;

namespace CurveEditor.UI
{
    public class UICurveEditorColors
    {
        public Color backgroundColor = new Color(0.721f, 0.682f, 0.741f);
    }

    public class UICurveLineColors
    {
        public Color pointColor = new Color(0.427f, 0.035f, 0.517f);
        public Color selectedPointColor = new Color(0.682f, 0.211f, 0.788f);
        public Color handleLineColor = new Color(0, 0, 0);
        public Color handleLineColorFree = new Color(0.427f, 0.035f, 0.517f);
        public Color inHandleColor = new Color(0, 0, 0);
        public Color inHandleColorWeighted = new Color(0.427f, 0.035f, 0.517f);
        public Color outHandleColor = new Color(0, 0, 0);
        public Color outHandleColorWeighted = new Color(0.427f, 0.035f, 0.517f);
        public Color lineColor = new Color(0.9f, 0.9f, 0.9f);

        public static UICurveLineColors CreateFrom(Color tint)
        {
            //TODO: proper palette generator
            float h, s, v;
            Color.RGBToHSV(tint, out h, out s, out v);

            var darkColor = Color.HSVToRGB(h, s, v * 0.8f);
            var veryDarkColor = Color.HSVToRGB(h, s, v * 0.5f);
            var desaturatedColor = Color.HSVToRGB(h, s * 0.5f, 1);

            return new UICurveLineColors()
            {
                pointColor = darkColor,
                selectedPointColor = tint,
                handleLineColor = veryDarkColor,
                handleLineColorFree = darkColor,
                inHandleColor = veryDarkColor,
                inHandleColorWeighted = darkColor,
                outHandleColor = veryDarkColor,
                outHandleColorWeighted = darkColor,
                lineColor = desaturatedColor
            };
        }
    }
}
