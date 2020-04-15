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
                if(containingAtom != null)
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
            _curveEditor.AddCurve(_curveJSON, UICurveLineColors.CreateFrom(new Color(0.388f, 0.698f, 0.890f)));
            _builder.CreateButton("Reset", () =>
            {
                _curveJSON.SetValToDefault();
                _curveEditor.UpdateCurve(_curveJSON);
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
        protected void OnDestroy() 
        {
            if (_animation != null)
                GameObject.Destroy(_animation);
        }

        private void CurveUpdated(AnimationCurve curve)
        {
            if (_animation == null)
                return;

            var playing = _animation.isPlaying;
            var time = _animation["CurveEditorDemo"]?.time ?? 0;
            var clip = new AnimationClip
            {
                legacy = true,
                wrapMode = WrapMode.Loop
            };
            clip.SetCurve("", typeof(Transform), "localPosition.x", curve);
            _animation.AddClip(clip, "CurveEditorDemo");
            if (playing)
            {
                _animation["CurveEditorDemo"].time = time;
                _animation.Play("CurveEditorDemo");
            }
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
            _curveEditor.UpdateCurve(_curveJSON);
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
            => RestoreFromJSON(jc, restorePhysical, restoreAppearance, null, setMissingToDefault);
    }
}
