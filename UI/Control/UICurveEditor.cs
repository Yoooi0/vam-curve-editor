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
        private readonly UICurveEditorColors _colors;

        private bool _readOnly;

        public bool readOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;

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

        public UICurveEditor(UIDynamic container, float width, float height, List<UIDynamicButton> buttons = null, UICurveEditorColors colors = null, bool readOnly = false)
        {
            var buttonContainerHeight = (buttons == null || buttons.Count == 0) ? 0 : 25;

            this.container = container;

            _colors = colors ?? new UICurveEditorColors();

            gameObject = new GameObject();
            gameObject.transform.SetParent(container.transform, false);

            var mask = gameObject.AddComponent<RectMask2D>();
            mask.rectTransform.anchoredPosition = new Vector2(0, buttonContainerHeight / 2);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            mask.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            var backgroundContent = new GameObject();
            backgroundContent.transform.SetParent(gameObject.transform, false);

            var backgroundImage = backgroundContent.AddComponent<Image>();
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            backgroundImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);
            backgroundImage.color = _colors.backgroundColor;

            _canvasContainer = new GameObject();
            _canvasContainer.transform.SetParent(gameObject.transform, false);
            _canvasContainer.AddComponent<CanvasGroup>();
            _canvas = _canvasContainer.AddComponent<UICurveEditorCanvas>();
            _canvas.rectTransform.anchorMin = new Vector2(0, 0);
            _canvas.rectTransform.anchorMax = new Vector2(0, 0);
            _canvas.rectTransform.pivot = new Vector2(0, 0);
            _canvas.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _canvas.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height - buttonContainerHeight);

            this.readOnly = readOnly;

            if (buttons != null && buttons.Count != 0)
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
        public void AddCurve(IStorableAnimationCurve storable, UICurveLineColors colors = null, float thickness = 0.04f) => _canvas.CreateCurve(storable, colors, thickness);
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
}
