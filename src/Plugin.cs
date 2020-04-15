using CurveEditor.UI;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CurveEditor
{
    public partial class Plugin : MVRScript
    {
        private UIBuilder _builder;

        public static readonly string PluginName = "Curve Editor Demo";
        public static readonly string PluginAuthor = "Yoooi";

        private UICurveEditor CurveEditor;
        private UIDynamicButton Button;
        private AnimationCurve Curve = new AnimationCurve();

        public override void Init()
        {
            try
            {
                ResetCurve();
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

            CurveEditor = _builder.CreateCurveEditor(300);
            CurveEditor.curve = Curve;
            Button = _builder.CreateButton("Reset", () =>
            {
                ResetCurve();
                CurveEditor.curve = Curve;
            });
        }

        private void ResetCurve()
        {
            Curve.keys = new[]
            {
                    new Keyframe(0, 0, 0, 1),
                    new Keyframe(1, 1, 1, 0)
                };
        }

        protected void Update() { }
        protected void OnDestroy() { }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jc = base.GetJSON(includePhysical, includeAppearance, forceStore);
            var jcCurve = new JSONClass();
            for (var i = 0; i < Curve.keys.Length; i++)
            {
                var k = Curve.keys[i];
                jcCurve[name][i] = $"{k.time}, {k.value}, {k.inTangent}, {k.outTangent}, {k.inWeight}, {k.outWeight}, {(int)k.weightedMode}";
            }
            jc["curve"] = jcCurve;
            needsStore = true;
            return jc;
        }

        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

            var jcCurve = jc["curve"];
            if (jcCurve != null)
            {
                var keyframes = new List<Keyframe>();
                var array = jc["curve"].AsArray;

                for (var i = 0; i < array.Count; i++)
                {
                    var values = jc[name][i].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s)).ToArray();
                    var key = new Keyframe(values[0], values[1], values[2], values[3], values[4], values[5]);
                    key.weightedMode = (WeightedMode)(int)values[6];
                    Curve.AddKey(key);
                }
            }
            else if (setMissingToDefault)
            {
                ResetCurve();
            }
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
            => RestoreFromJSON(jc, restorePhysical, restoreAppearance, null, setMissingToDefault);
    }
}
