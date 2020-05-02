using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace CurveEditor.UI
{
    public class UICurveEditor
    {
        public readonly UIDynamic container;
        public readonly GameObject gameObject;

        private readonly GameObject _canvasContainer;
        private readonly UICurveEditorCanvas _canvas;
        public UICurveEditorSettings settings { get; private set; }

        public UICurveEditor(UIDynamic container, float width, float height, List<UIDynamicButton> buttons = null, UICurveEditorSettings settings = null)
        {
            this.container = container;
            this.settings = settings ?? UICurveEditorSettings.Default();
            this.settings.PropertyChanged += OnSettingsChanged;

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var buttonContainerHeight = (buttons == null || buttons.Count == 0) ? 0 : this.settings.buttonContainerHeight;
            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, buttonContainerHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            var backgroundContent = new GameObject();
            backgroundContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);
            backgroundImage.color = this.settings.backgroundColor;

            _canvasContainer = new GameObject();
            _canvasContainer.transform.SetParent(gameObject.transform, false);
            _canvasContainer.AddComponent<CanvasGroup>();
            _canvas = _canvasContainer.AddComponent<UICurveEditorCanvas>();
            _canvas.settings = this.settings;

            _canvas.rectTransform.anchorMin = new Vector2(0, 0);
            _canvas.rectTransform.anchorMax = new Vector2(0, 0);
            _canvas.rectTransform.pivot = new Vector2(0, 0);
            _canvas.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _canvas.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            if (buttonContainerHeight > 0)
            {
                var buttonContainer = new GameObject();
                buttonContainer.transform.SetParent(container.transform, false);

                var rectTransform = buttonContainer.AddComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(0, -(height - buttonContainerHeight) / 2);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, buttonContainerHeight);

                var gridLayout = buttonContainer.AddComponent<GridLayoutGroup>();
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = buttons.Count;
                gridLayout.spacing = new Vector2();
                gridLayout.cellSize = new Vector2(width / buttons.Count, buttonContainerHeight);
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                foreach (var button in buttons)
                    button.gameObject.transform.SetParent(gridLayout.transform, false);
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "readOnly")
            {
                var canvasGroup = _canvasContainer.GetComponent<CanvasGroup>();
                canvasGroup.interactable = !settings.readOnly;
                canvasGroup.blocksRaycasts = !settings.readOnly;
                _canvas.SetSelectedPoint(null);
            }
        }

        //TODO: meh...
        public void AddCurve(IStorableAnimationCurve storable, UICurveLineSettings settings = null) => _canvas.CreateCurve(storable, settings);
        public void RemoveCurve(IStorableAnimationCurve storable) => _canvas.RemoveCurve(storable);
        public void UpdateCurve(IStorableAnimationCurve storable) => _canvas.UpdateCurve(storable);
        public void SetScrubberPosition(float time) => _canvas.SetScrubberPosition(time);
        public void SetScrubber(IStorableAnimationCurve storable, float time) => _canvas.SetScrubberPosition(storable, time);
        public void SetValueBounds(IStorableAnimationCurve storable, Rect valueBounds, bool normalizeToView = false, bool offsetToCenter = false) => _canvas.SetValueBounds(storable, valueBounds, normalizeToView, offsetToCenter);
        public void SetValueBounds(IStorableAnimationCurve storable, Vector2 min, Vector2 max, bool normalizeToView = false, bool offsetToCenter = false) => _canvas.SetValueBounds(storable, min, max, normalizeToView, offsetToCenter);
        public void SetViewToFit(Vector4 margin = new Vector4()) => _canvas.SetViewToFit(margin);
        public void ToggleHandleMode() => _canvas.ToggleHandleMode();
        public void ToggleOutHandleMode() => _canvas.ToggleOutHandleMode();
        public void ToggleInHandleMode() => _canvas.ToggleInHandleMode();
        public void SetLinear() => _canvas.SetLinear();
    }

    public class UICurveEditorSettings : INotifyPropertyChanged
    {
        private float _buttonContainerHeight = 25;
        private Color _backgroundColor = new Color(0.721f, 0.682f, 0.741f);
        private bool _showScrubbers = true;
        private bool _showGrid = true;
        private Color _gridColor = new Color(0.6f, 0.6f, 0.6f);
        private Color _gridAxisColor = new Color(0.5f, 0.5f, 0.5f);
        private float _gridThickness = 0.01f;
        private float _gridAxisThickness = 0.04f;
        private int _gridCellCount = 10;
        private bool _allowViewDragging = true;
        private bool _allowViewZooming = true;
        private bool _allowViewScaling = true;
        private bool _allowKeyboardShortcuts = true;
        private bool _readOnly = false;

        protected UICurveEditorSettings() { }

        public static UICurveEditorSettings Default() => new UICurveEditorSettings();

        #region INotifyPropertyChanged
        public float buttonContainerHeight { get { return _buttonContainerHeight; } set { Set(ref _buttonContainerHeight, value, nameof(buttonContainerHeight)); } }
        public Color backgroundColor { get { return _backgroundColor; } set { Set(ref _backgroundColor, value, nameof(backgroundColor)); } }

        public bool showScrubbers { get { return _showScrubbers; } set { Set(ref _showScrubbers, value, nameof(showScrubbers)); } }
        public bool showGrid { get { return _showGrid; } set { Set(ref _showGrid, value, nameof(showGrid)); } }
        public Color gridColor { get { return _gridColor; } set { Set(ref _gridColor, value, nameof(gridColor)); } }
        public Color gridAxisColor { get { return _gridAxisColor; } set { Set(ref _gridAxisColor, value, nameof(gridAxisColor)); } }
        public float gridThickness { get { return _gridThickness; } set { Set(ref _gridThickness, value, nameof(gridThickness)); } }
        public float gridAxisThickness { get { return _gridAxisThickness; } set { Set(ref _gridAxisThickness, value, nameof(gridAxisThickness)); } }
        public int gridCellCount { get { return _gridCellCount; } set { Set(ref _gridCellCount, value, nameof(gridCellCount)); } }

        public bool allowViewDragging { get { return _allowViewDragging; } set { Set(ref _allowViewDragging, value, nameof(allowViewDragging)); } }
        public bool allowViewZooming { get { return _allowViewZooming; } set { Set(ref _allowViewZooming, value, nameof(allowViewZooming)); } }
        public bool allowViewScaling { get { return _allowViewScaling; } set { Set(ref _allowViewScaling, value, nameof(allowViewScaling)); } }
        public bool allowKeyboardShortcuts { get { return _allowKeyboardShortcuts; } set { Set(ref _allowKeyboardShortcuts, value, nameof(allowKeyboardShortcuts)); } }
        public bool readOnly { get { return _readOnly; } set { Set(ref _readOnly, value, nameof(readOnly)); } }

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