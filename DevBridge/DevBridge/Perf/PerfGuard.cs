using UnityEngine;

namespace DevBridge.Perf
{
    /// <summary>
    /// Takes the probes off when the sample's coroutine cannot: Unity stops a coroutine without running its finally
    /// blocks when its object is disabled or destroyed, or when its coroutines are stopped. Lives on DevBridge's own
    /// object. Not at quit, when the process is ending anyway.
    /// </summary>
    internal sealed class PerfGuard : MonoBehaviour
    {
        /// <summary>A live sample yields every frame; this many frames without a step means its coroutine is gone.</summary>
        private const int SilentFrames = 5;

        private static bool quitting;

        internal static void Watch()
        {
            GameObject host = DevBridgePlugin.Instance.gameObject;
            if (!host.GetComponent<PerfGuard>()) host.AddComponent<PerfGuard>();
        }

        private void Update()
        {
            PerfSession session = PerfSession.Current;
            if (session != null && Time.frameCount - session.BeatFrame > SilentFrames)
                PerfSession.Abort("its coroutine stopped running");
        }

        private void OnApplicationQuit() => quitting = true;

        private void OnDisable()
        {
            if (!quitting) PerfSession.Abort("DevBridge's object was disabled or destroyed");
        }

        private void OnDestroy()
        {
            if (!quitting) PerfSession.Abort("DevBridge's object was destroyed");
        }
    }
}
