
using System;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace CurveEditor
{
    public class JSONStorableAnimationCurve : JSONStorableParam, IStorableAnimationCurve
    {
        public delegate void AnimationCurveUpdatedCallback(AnimationCurve val);

        public Keyframe[] _defaultVal = new[]
        {
            new Keyframe(0, 0),
            new Keyframe(1, 1)
        };

        public AnimationCurve val { get; set; } = new AnimationCurve();

        private AnimationCurveUpdatedCallback updatedCallbackFunction;

        public JSONStorableAnimationCurve(string paramName)
            : this(paramName, new AnimationCurve(), null)
        {
        }

        public JSONStorableAnimationCurve(string paramName, AnimationCurveUpdatedCallback callback)
            : this(paramName, new AnimationCurve(), callback)
        {
        }

        public JSONStorableAnimationCurve(string paramName, AnimationCurve curve)
            : this(paramName, curve, null)
        {
        }

        public JSONStorableAnimationCurve(string paramName, AnimationCurve curve, AnimationCurveUpdatedCallback callback)
        {
            name = paramName;
            val = curve;
            updatedCallbackFunction = callback;
        }

        public override bool StoreJSON(JSONClass jc, bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            // NOTE: We cannot easily compare if the curve is "not the default" so we always save
            var flag = NeedsStore(jc, includePhysical, includeAppearance) || (forceStore || true);
            if (flag)
            {
                for (var i = 0; i < val.keys.Length; i++)
                {
                    var k = val.keys[i];
                    jc[name][i] = $"{k.time}, {k.value}, {k.inTangent}, {k.outTangent}, {k.inWeight}, {k.outWeight}, {(int)k.weightedMode}";
                }
            }
            return flag;
        }

        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
        {
            if (!NeedsRestore(jc, restorePhysical, restoreAppearance))
                return;

            if (jc[name] != null)
            {
                while (val.length > 0)
                    val.RemoveKey(0);

                var array = jc[name].AsArray;

                for (var i = 0; i < array.Count; i++)
                {
                    var values = jc[name][i].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(s => float.Parse(s)).ToArray();
                    var key = new Keyframe(values[0], values[1], values[2], values[3], values[4], values[5]);
                    key.weightedMode = (WeightedMode)(int)values[6];
                    val.AddKey(key);
                }
            }
            else if (setMissingToDefault)
            {
                SetValToDefault();
            }

            updatedCallbackFunction?.Invoke(val);
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
            => RestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);

        public override void SetValToDefault()
        {
            while (val.length > 0)
                val.RemoveKey(0);

            val.keys = _defaultVal;

            updatedCallbackFunction?.Invoke(val);
        }

        public override void SetDefaultFromCurrent()
        {
            _defaultVal = val.keys;
        }
    }
}
