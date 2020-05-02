using System.Collections.Generic;
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
        private readonly UICurveEditorSettings _settings;

        public bool readOnly
        {
            get { return _canvas.readOnly; }
            set
            {
                var canvasGroup = _canvasContainer.GetComponent<CanvasGroup>();
                canvasGroup.interactable = !value;
                canvasGroup.blocksRaycasts = !value;

                _canvas.readOnly = value;
                _canvas.SetSelectedPoint(null);
            }
        }

        public bool allowKeyboardShortcuts
        {
            get { return _canvas.allowKeyboardShortcuts; }
            set { _canvas.allowKeyboardShortcuts = value; }
        }

        public bool showScrubbers
        {
            get { return _canvas.showScrubbers; }
            set { _canvas.showScrubbers = value; }
        }

        public bool allowViewDragging
        {
            get { return _canvas.allowViewDragging; }
            set { _canvas.allowViewDragging = value; }
        }

        public bool allowViewZooming
        {
            get { return _canvas.allowViewZooming; }
            set { _canvas.allowViewZooming = value; }
        }

        public bool allowViewScaling
        {
            get { return _canvas.allowViewScaling; }
            set { _canvas.allowViewScaling = value; }
        }

        public UICurveEditor(UIDynamic container, float width, float height, List<UIDynamicButton> buttons = null, UICurveEditorSettings settings = null)
        {
            this.container = container;
            _settings = settings ?? UICurveEditorSettings.Default();

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var buttonContainerHeight = (buttons == null || buttons.Count == 0) ? 0 : _settings.buttonContainerHeight;
            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, buttonContainerHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            var backgroundContent = new GameObject();
            backgroundContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);
            backgroundImage.color = _settings.backgroundColor;

            _canvasContainer = new GameObject();
            _canvasContainer.transform.SetParent(gameObject.transform, false);
            _canvasContainer.AddComponent<CanvasGroup>();
            _canvas = _canvasContainer.AddComponent<UICurveEditorCanvas>();

            //TODO: 
            _canvas.showScrubbers = _settings.showScrubbers;
            _canvas.showGrid = _settings.showGrid;
            _canvas.gridColor = _settings.gridColor;
            _canvas.gridAxisColor = _settings.gridAxisColor;
            _canvas.zoom = _settings.defaultZoom;

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

    public class UICurveEditorSettings
    {
        public float buttonContainerHeight = 25;
        public Color backgroundColor = new Color(0.721f, 0.682f, 0.741f);
        public float defaultZoom = 100;

        //TODO: per curve?
        public bool showScrubbers = true;
        public bool showGrid = true;
        public Color gridColor = new Color(0.6f, 0.6f, 0.6f);
        public Color gridAxisColor = new Color(0.5f, 0.5f, 0.5f);

        protected UICurveEditorSettings() { }

        public static UICurveEditorSettings Default() => new UICurveEditorSettings();
    }
}
