using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Taming
{
    /// <summary>The tame watch's clock, on the plugin's own object: a time test every frame, the watch once a second.</summary>
    internal sealed class TameTicker : MonoBehaviour
    {
        private const float Seconds = 1f;

        private float _next;

        private void Update()
        {
            float now = Time.time;
            if (now < _next)
            {
                return;
            }
            _next = now + Seconds;
            Guard.Run("tame watch", TameWatch.Tick);
        }
    }
}
