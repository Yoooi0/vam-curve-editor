using CurveEditorDemo.UI;
using System;
using UnityEngine;

namespace CurveEditorDemo
{
    public partial class Plugin : MVRScript
    {
        private UIBuilder _builder;

        public static readonly string PluginName = "Curve Editor Demo";
        public static readonly string PluginAuthor = "Yoooi";

        private UICurveEditor CurveEditor;
        private UIDynamicButton Button;

        public override void Init()
        {
            try
            {
                CreateUI();
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }


        public void CreateUI()
        {
            pluginLabelJSON.val = PluginName;

            _builder = new UIBuilder(this);

            CurveEditor = _builder.CreateCurveEditor("Editor", 300);
            Button = _builder.CreateButton("Reset", () =>
            {
                var curve = new AnimationCurve();
                curve.AddKey(new Keyframe(0, 0, 0, 1));
                curve.AddKey(new Keyframe(1, 1, 1, 0));
                CurveEditor.curve = curve;
            });
        }

        protected void Update() { }
        protected void OnDestroy() { }
    }
}