using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// The raids' one clock, on the plugin's own object: a time test every frame, and every quarter second the hosts
    /// this machine holds tick (<see cref="RaidHosts.TickAll"/>; only an owner's does anything) and this machine's player
    /// reports to the raids it is at (<see cref="RaidHere"/>, every few seconds). One timer for the whole feature, not one
    /// per chest, and with no host loaded a tick costs a loop over an empty list.
    /// </summary>
    internal sealed class RaidTicker : MonoBehaviour
    {
        private float _next;

        private void Update()
        {
            float now = Time.time;
            if (now < _next)
            {
                return;
            }
            _next = now + RaidTable.TickSeconds;
            Guard.Run("raid tick", static t => Tick(t), now);
        }

        private static void Tick(float now)
        {
            if (ZNet.instance == null)
            {
                return;
            }
            RaidHosts.TickAll();
            RaidHere.PingDue(now);
        }
    }
}
