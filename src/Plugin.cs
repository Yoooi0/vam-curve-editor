using CurveEditor.UI;
using SimpleJSON;
using System;
using UnityEngine;

namespace CurveEditor
{
    public partial class Plugin : MVRScript
    {
        private UIBuilder _builder;

        public static readonly string PluginName = "Curve Editor Demo";
        public static readonly string PluginAuthor = "Yoooi";

        private UICurveEditor _curveEditor;
        private Animation _animation;
        private JSONStorableAnimationCurve _curveJSON;

        public override void Init()
        {
            try
            {
                _animation = containingAtom.GetComponent<Animation>() ?? containingAtom.gameObject.AddComponent<Animation>();
                _curveJSON = new JSONStorableAnimationCurve("Curve", CurveUpdated);
                _curveJSON.SetValToDefault();
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

            _curveEditor = _builder.CreateCurveEditor(300);
            _curveEditor.AddCurve(_curveJSON);
            _builder.CreateButton("Reset", () =>
            {
                _curveJSON.SetValToDefault();
                _curveEditor.UpdatePoints(_curveJSON);
            });
            _builder.CreateButton("Reset", () =>
            {
                _curveJSON.SetValToDefault();
                _curveEditor.UpdatePoints(_curveJSON);
            });
            _builder.CreateButton("Play", () =>
            {
                _animation.Play("CurveEditorDemo");
            });
            _builder.CreateButton("Stop", () =>
            {
                _animation.Stop();
            });
        }

        protected void Update() { }
        protected void OnDestroy() { }

        private void CurveUpdated(AnimationCurve curve)
        {
            var clip = new AnimationClip
            {
                legacy = true,
                wrapMode = WrapMode.Loop
            };
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            _animation.AddClip(clip, "CurveEditorDemo");
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            var jc = base.GetJSON(includePhysical, includeAppearance, forceStore);
            if (_curveJSON.StoreJSON(jc, includePhysical, includeAppearance, forceStore)) needsStore = true;
            return jc;
        }

        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

            _curveJSON.RestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);
            _curveEditor.UpdatePoints(_curveJSON);
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
            => RestoreFromJSON(jc, restorePhysical, restoreAppearance, null, setMissingToDefault);
    }
}
