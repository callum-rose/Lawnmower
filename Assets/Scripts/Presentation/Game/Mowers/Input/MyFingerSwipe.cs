using Lean.Touch;
using UnityEngine;

namespace Game.Mowers.Input
{
    internal class MyFingerSwipe : LeanFingerSwipe
    {
        [SerializeField, Range(0, 1)] private float swipeMinStraightnessThreshold = 0.8f;
        [SerializeField, Min(1)] private int snapShotSkips = 1;

        protected override void OnEnable()
        {
            LeanTouch.OnFingerSwipe += LeanTouch_OnFingerSwipe;
        }

        protected override void OnDisable()
        {
            LeanTouch.OnFingerSwipe -= LeanTouch_OnFingerSwipe;
        }

        private void LeanTouch_OnFingerSwipe(LeanFinger finger)
        {
            if (!IsSwipeStraightEnough(finger))
            {
                return;
            }

            HandleFingerSwipe(finger);
        }

        private bool IsSwipeStraightEnough(LeanFinger finger)
        {
            float totalDistance = CalcFingerTotalDistance(finger);
            float fingerDisplacement = (finger.ScreenPosition - finger.StartScreenPosition).magnitude;

            return fingerDisplacement >= totalDistance * swipeMinStraightnessThreshold;
        }

        private float CalcFingerTotalDistance(LeanFinger finger)
        {
            float totalDistance = 0;
            for (int i = 1; i < finger.Snapshots.Count; i += snapShotSkips)
            {
                LeanSnapshot prevSnap = finger.Snapshots[i - 1];
                LeanSnapshot currentSnap = finger.Snapshots[i];

                Vector2 displacement = currentSnap.ScreenPosition - prevSnap.ScreenPosition;
                totalDistance += displacement.magnitude;
            }

            return totalDistance;
        }
    }
}