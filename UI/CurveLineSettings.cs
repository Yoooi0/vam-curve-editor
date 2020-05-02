using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace CurveEditor.UI
{
    public class CurveLineSettings : INotifyPropertyChanged
    {
        private Color _pointDotColor = new Color(0.427f, 0.035f, 0.517f);
        private Color _pointDotColorSelected = new Color(0.682f, 0.211f, 0.788f);
        private Color _pointHandleLineColor = new Color(0, 0, 0);
        private Color _pointHandleLineColorFree = new Color(0.427f, 0.035f, 0.517f);
        private Color _pointHandleDotColor = new Color(0, 0, 0);
        private Color _pointHandleDotColorWeighted = new Color(0.427f, 0.035f, 0.517f);

        private float _pointDotRadius = 0.08f;
        private float _pointDotSkin = 0.04f;
        private float _pointHandleDotRadius = 0.07f;
        private float _pointHandleDotSkin = 0.04f;
        private float _pointShellSize = 0.2f;
        private float _pointHandleLineThickness = 0.03f;
        private float _defaultPointHandleLength = 0.5f;

        private float _curveLinePrecision = 0.01f;
        private float _curveLineThickness = 0.04f;
        private int _curveLineEvaluateCount = 100;
        private Color _curveLineColor = new Color(0.9f, 0.9f, 0.9f);
        private Color _scrubberColor = new Color(0.382f, 0.111f, 0.488f);
        private float _scrubberLineThickness = 0.02f;
        private float _scrubberPointRadius = 0.03f;

        protected CurveLineSettings() { }

        public static CurveLineSettings Default() => new CurveLineSettings();
        public static CurveLineSettings Colorize(Color tint, CurveLineSettings settings = null)
        {
            //TODO: proper palette generator

            settings = settings ?? Default();

            float h, s, v;
            Color.RGBToHSV(tint, out h, out s, out v);

            var darkColor = Color.HSVToRGB(h, s, v * 0.8f);
            var veryDarkColor = Color.HSVToRGB(h, s, v * 0.5f);
            var desaturatedColor = Color.HSVToRGB(h, s * 0.5f, 1);

            settings.pointDotColor = darkColor;
            settings.pointDotColorSelected = tint;
            settings.pointHandleLineColor = veryDarkColor;
            settings.pointHandleLineColorFree = darkColor;
            settings.pointHandleDotColor = veryDarkColor;
            settings.pointHandleDotColorWeighted = darkColor;
            settings.curveLineColor = desaturatedColor;
            settings.scrubberColor = Color.HSVToRGB(h, s * 1.2f, v * 0.9f);

            return settings;
        }

        #region INotifyPropertyChanged
        public Color pointDotColor { get { return _pointDotColor; } set { Set(ref _pointDotColor, value, nameof(pointDotColor)); } }
        public Color pointDotColorSelected { get { return _pointDotColorSelected; } set { Set(ref _pointDotColorSelected, value, nameof(pointDotColorSelected)); } }
        public Color pointHandleLineColor { get { return _pointHandleLineColor; } set { Set(ref _pointHandleLineColor, value, nameof(pointHandleLineColor)); } }
        public Color pointHandleLineColorFree { get { return _pointHandleLineColorFree; } set { Set(ref _pointHandleLineColorFree, value, nameof(pointHandleLineColorFree)); } }
        public Color pointHandleDotColor { get { return _pointHandleDotColor; } set { Set(ref _pointHandleDotColor, value, nameof(pointHandleDotColor)); } }
        public Color pointHandleDotColorWeighted { get { return _pointHandleDotColorWeighted; } set { Set(ref _pointHandleDotColorWeighted, value, nameof(pointHandleDotColorWeighted)); } }

        public float pointDotRadius { get { return _pointDotRadius; } set { Set(ref _pointDotRadius, value, nameof(pointDotRadius)); } }
        public float pointDotSkin { get { return _pointDotSkin; } set { Set(ref _pointDotSkin, value, nameof(pointDotSkin)); } }
        public float pointHandleDotRadius { get { return _pointHandleDotRadius; } set { Set(ref _pointHandleDotRadius, value, nameof(pointHandleDotRadius)); } }
        public float pointHandleDotSkin { get { return _pointHandleDotSkin; } set { Set(ref _pointHandleDotSkin, value, nameof(pointHandleDotSkin)); } }
        public float pointShellSize { get { return _pointShellSize; } set { Set(ref _pointShellSize, value, nameof(pointShellSize)); } }
        public float pointHandleLineThickness { get { return _pointHandleLineThickness; } set { Set(ref _pointHandleLineThickness, value, nameof(pointHandleLineThickness)); } }
        public float defaultPointHandleLength { get { return _defaultPointHandleLength; } set { Set(ref _defaultPointHandleLength, value, nameof(defaultPointHandleLength)); } }

        public float curveLinePrecision { get { return _curveLinePrecision; } set { Set(ref _curveLinePrecision, value, nameof(curveLinePrecision)); } }
        public float curveLineThickness { get { return _curveLineThickness; } set { Set(ref _curveLineThickness, value, nameof(curveLineThickness)); } }
        public int curveLineEvaluateCount { get { return _curveLineEvaluateCount; } set { Set(ref _curveLineEvaluateCount, value, nameof(curveLineEvaluateCount)); } }
        public Color curveLineColor { get { return _curveLineColor; } set { Set(ref _curveLineColor, value, nameof(curveLineColor)); } }
        public Color scrubberColor { get { return _scrubberColor; } set { Set(ref _scrubberColor, value, nameof(scrubberColor)); } }
        public float scrubberLineThickness { get { return _scrubberLineThickness; } set { Set(ref _scrubberLineThickness, value, nameof(scrubberLineThickness)); } }
        public float scrubberPointRadius { get { return _scrubberPointRadius; } set { Set(ref _scrubberPointRadius, value, nameof(scrubberPointRadius)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool Set<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}

