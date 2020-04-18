using CurveEditor.UI;
using SimpleJSON;
using System;
using System.Linq;
using UnityEngine;

namespace CurveEditor
{
    public partial class Plugin : MVRScript
    {
        public static readonly string PluginName = "Curve Editor Demo";
        public static readonly string PluginAuthor = "Yoooi";

        private UICurveEditor _curveEditor;
        private Animation _animation;
        private JSONStorableAnimationCurve _curveJSON, _curve2JSON;

        public override void Init()
        {
            try
            {
                if(containingAtom != null)
                    _animation = containingAtom.GetComponent<Animation>() ?? containingAtom.gameObject.AddComponent<Animation>();

                _curveJSON = new JSONStorableAnimationCurve("Curve", CurveUpdated);
                _curveJSON.val = AnimationCurve.EaseInOut(0, 0, 2, 1);
                _curve2JSON = new JSONStorableAnimationCurve("Curve", CurveUpdated);
                _curve2JSON.val = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);

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

            var container = CreateSpacer();
            container.height = 300;

            var curveEditorButtons = Enumerable.Range(0, 4)
                .Select(_ => UnityEngine.Object.Instantiate(manager.configurableButtonPrefab))
                .Select(t => t.GetComponent<UIDynamicButton>())
                .ToList();

            foreach (var b in curveEditorButtons)
            {
                b.buttonText.fontSize = 18;
                b.buttonColor = Color.white;
            }

            curveEditorButtons[0].label = "Mode";
            curveEditorButtons[1].label = "In Mode";
            curveEditorButtons[2].label = "Out Mode";
            curveEditorButtons[3].label = "Linear";

            curveEditorButtons[0].button.onClick.AddListener(() => _curveEditor.ToggleHandleMode());
            curveEditorButtons[1].button.onClick.AddListener(() => _curveEditor.ToggleInHandleMode());
            curveEditorButtons[2].button.onClick.AddListener(() => _curveEditor.ToggleOutHandleMode());
            curveEditorButtons[3].button.onClick.AddListener(() => _curveEditor.SetLinear());

            _curveEditor = new UICurveEditor(container, 520, container.height, buttons: curveEditorButtons);
            _curveEditor.AddCurve(_curveJSON, UICurveLineColors.CreateFrom(new Color(0.388f, 0.698f, 0.890f)));
            _curveEditor.AddCurve(_curve2JSON, UICurveLineColors.CreateFrom(new Color(0.890f, 0.388f, 0.398f)));

            var resetButton = CreateButton("Reset");
            var playButton = CreateButton("Play");
            var stopButton = CreateButton("Stop");

            resetButton.button.onClick.AddListener(() =>
            {
                _curveJSON.SetValToDefault();
                _curveEditor.UpdateCurve(_curveJSON);
            });
            playButton.button.onClick.AddListener(() =>
            {
                _animation.Play("CurveEditorDemo");
            });
            stopButton.button.onClick.AddListener(() =>
            {
                _animation.Stop();
            });

            var readOnlyStorable = new JSONStorableBool("ReadOnly", false);
            var readOnlyToggle = CreateToggle(readOnlyStorable);
            readOnlyStorable.setCallbackFunction = v => _curveEditor.readOnly = v;

            var showScrubberStorable = new JSONStorableBool("Show Scrubbers", true);
            var showScrubberToggle = CreateToggle(showScrubberStorable);
            showScrubberStorable.setCallbackFunction = v => _curveEditor.showScrubbers = v;

            var sliderStorable = new JSONStorableFloat("Scrubber time", 0, 0, 1);
            var slider = CreateSlider(sliderStorable);

            sliderStorable.setCallbackFunction = v =>
            {
                _curveEditor.SetScrubber(_curveJSON, v);
                _curveEditor.SetScrubber(_curve2JSON, 1 - v);
            };
        }

        protected void Update() { }
        protected void OnDestroy()
        {
            if (_animation != null)
                GameObject.Destroy(_animation);
            RemoveSpacer(_curveEditor.container);
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
