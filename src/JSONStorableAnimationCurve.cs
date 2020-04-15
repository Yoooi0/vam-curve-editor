
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

        private readonly AnimationCurveUpdatedCallback _updatedCallbackFunction;

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
            _updatedCallbackFunction = callback;
        }

        public void NotifyUpdated()
        {
            _updatedCallbackFunction?.Invoke(val);
        }

        public override bool StoreJSON(JSONClass jc, bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            // NOTE: We cannot easily compare if the curve is "not the default" so we always save
            var flag = NeedsStore(jc, includePhysical, includeAppearance) || (forceStore || true);
            if (flag)
            {
                var jcCurve = new JSONClass();
                var jcKeyframes = new JSONArray();
                for (var i = 0; i < val.length; i++)
                {
                    var keyframe = val[i];
                    var jcKeyframe = new JSONClass();
                    jcKeyframe["time"].AsFloat = keyframe.time;
                    jcKeyframe["value"].AsFloat = keyframe.value;
                    jcKeyframe["inTangent"].AsFloat = keyframe.inTangent;
                    jcKeyframe["outTangent"].AsFloat = keyframe.outTangent;
                    jcKeyframe["inWeight"].AsFloat = keyframe.inWeight;
                    jcKeyframe["outWeight"].AsFloat = keyframe.outWeight;
                    jcKeyframe["weightedMode"].AsInt = (int)keyframe.weightedMode;
                    jcKeyframes.Add(jcKeyframe);
                }
                jcCurve["keyframes"] = jcKeyframes;
                jc[name] = jcCurve;
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

                var jcCurve = jc[name];
                var jcKeyframes = jcCurve["keyframes"].AsArray;

                for (var i = 0; i < jcKeyframes.Count; i++)
                {
                    var jcKeyframe = jcKeyframes[i];
                    var key = new Keyframe(
                        jcKeyframe["time"].AsFloat,
                        jcKeyframe["value"].AsFloat,
                        jcKeyframe["inTangent"].AsFloat,
                        jcKeyframe["outTangent"].AsFloat,
                        jcKeyframe["inWeight"].AsFloat,
                        jcKeyframe["outWeight"].AsFloat)
                    {
                        weightedMode = (WeightedMode)jcKeyframe["weightedMode"].AsInt
                    };
                    val.AddKey(key);
                }
            }
            else if (setMissingToDefault)
            {
                SetValToDefault();
            }

            NotifyUpdated();
        }

        public override void LateRestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, bool setMissingToDefault = true)
            => RestoreFromJSON(jc, restorePhysical, restoreAppearance, setMissingToDefault);

        public override void SetValToDefault()
        {
            while (val.length > 0)
                val.RemoveKey(0);

            val.keys = _defaultVal;

            NotifyUpdated();
        }

        public override void SetDefaultFromCurrent()
        {
            _defaultVal = val.keys;
        }
    }
}
