#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Game.Haptics
{
    /// <summary>
    /// Taptic Engine feedback. Calls the native plugin in Assets/Plugins/iOS on iOS players and
    /// does nothing in the Editor and on every other platform.
    /// </summary>
    public static class HapticFeedback
    {
        /// <summary>When false every call is ignored. Bound to the Settings haptics toggle.</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>A light tap, for a move.</summary>
        public static void LightTap()
        {
            if (!Enabled)
                return;
#if UNITY_IOS && !UNITY_EDITOR
            LawnmowerHaptics_LightTap();
#endif
        }

        /// <summary>The success notification, for a win.</summary>
        public static void Success()
        {
            if (!Enabled)
                return;
#if UNITY_IOS && !UNITY_EDITOR
            LawnmowerHaptics_Success();
#endif
        }

        /// <summary>The error notification, for a fail.</summary>
        public static void Error()
        {
            if (!Enabled)
                return;
#if UNITY_IOS && !UNITY_EDITOR
            LawnmowerHaptics_Error();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void LawnmowerHaptics_LightTap();
        [DllImport("__Internal")] private static extern void LawnmowerHaptics_Success();
        [DllImport("__Internal")] private static extern void LawnmowerHaptics_Error();
#endif
    }
}
