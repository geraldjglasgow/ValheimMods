using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Tally
{
    /// <summary>The boss tallies' write timer, on the plugin's own object: one test a frame while no boss is being hit.</summary>
    internal sealed class TallyFlush : MonoBehaviour
    {
        private void Update() => Guard.Run("DamageTally.Flush", static now => DamageTally.FlushDue(now), Time.time);
    }
}
