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
        public UICurveEditorSettings settings { get; }

        public UICurveEditor(UIDynamic container, float width, float height, List<UIDynamicButton> buttons = null, UICurveEditorSettings settings = null)
        {
            this.container = container;
            this.settings = settings ?? new UICurveEditorSettings();
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

            if (buttons != null && buttonContainerHeight > 0)
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

            OnSettingsChanged(this, new PropertyChangedEventArgs(null));
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || e.PropertyName == nameof(UICurveEditorSettings.readOnly))
            {
                var canvasGroup = _canvasContainer.GetComponent<CanvasGroup>();
                canvasGroup.interactable = !settings.readOnly;
                canvasGroup.blocksRaycasts = !settings.readOnly;
                _canvas.SetSelectedPoint(null);
            }
        }

        //TODO: meh...
        public void AddCurve(IStorableAnimationCurve storable, CurveLineSettings settings = null) => _canvas.CreateCurve(storable, settings);
        public void RemoveCurve(IStorableAnimationCurve storable) => _canvas.RemoveCurve(storable);
        public void UpdateCurve(IStorableAnimationCurve storable) => _canvas.UpdateCurve(storable);
        public void SetScrubberPosition(float time) => _canvas.SetScrubberPosition(time);
        public void SetScrubberPosition(IStorableAnimationCurve storable, float time) => _canvas.SetScrubberPosition(storable, time);
        public void SetDrawScale(IStorableAnimationCurve storable, Rect valueBounds, bool normalizeToView = false, bool offsetToCenter = false) => _canvas.SetDrawScale(storable, valueBounds, normalizeToView, offsetToCenter);
        public void SetDrawScale(IStorableAnimationCurve storable, Vector2 min, Vector2 max, bool normalizeToView = false, bool offsetToCenter = false) => SetDrawScale(storable, new Rect(min, max - min), normalizeToView, offsetToCenter);
        public void SetViewToFit(Vector4 margin = new Vector4()) => _canvas.SetViewToFit(margin);
        public void ToggleHandleMode() => _canvas.ToggleHandleMode();
        public void ToggleOutHandleMode() => _canvas.ToggleOutHandleMode();
        public void ToggleInHandleMode() => _canvas.ToggleInHandleMode();
        public void SetLinear() => _canvas.SetLinear();
    }
}