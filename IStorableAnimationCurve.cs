
using UnityEngine;

namespace CurveEditor
{
    public interface IStorableAnimationCurve
    {
        AnimationCurve val { get; }

        void NotifyUpdated();
    }
}
