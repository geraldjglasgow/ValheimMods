using UnityEngine;

namespace DevBridge.Perf
{
    /// <summary>
    /// Takes the probes off when a sample's or a recording's coroutine cannot: Unity stops a coroutine without running its
    /// finally blocks when its object is disabled or destroyed, or when its coroutines are stopped. Lives on DevBridge's
    /// own object. At quit the probes stay (the process is ending anyway), but a recording's files are closed.
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
            PerfRecording recording = PerfRecording.Current;
            if (recording != null && recording.Silent(SilentFrames)) recording.Abort("its coroutine stopped running");
        }

        private void OnApplicationQuit()
        {
            quitting = true;
            PerfRecording.Current?.AtQuit();
        }

        private void OnDisable() => Gone("DevBridge's object was disabled or destroyed");

        private void OnDestroy() => Gone("DevBridge's object was destroyed");

        private static void Gone(string why)
        {
            if (quitting) return;
            PerfSession.Abort(why);
            PerfRecording.Current?.Abort(why);
        }
    }
}
