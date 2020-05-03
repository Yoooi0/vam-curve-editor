using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace CurveEditor.UI
{
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