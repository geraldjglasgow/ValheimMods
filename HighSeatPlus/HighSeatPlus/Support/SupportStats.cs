using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// Counters for the in-game check of building support checked on change, read with DevBridge
    /// (<c>/eval?expr=HighSeatPlus.SupportStats.Skipped</c>), and the warning for a safety recheck that found a support
    /// value nothing announced. Counted per call of WearNTear.UpdateSupport on this PC's own pieces:
    /// <list type="bullet">
    /// <item><see cref="Skipped"/>: skipped, the record held.</item>
    /// <item><see cref="AfterChange"/>: the game's check ran because something the record watches changed.</item>
    /// <item><see cref="Rechecked"/>: the game's check ran because the record was <see cref="SupportSkip"/>'s safety
    /// interval old.</item>
    /// <item><see cref="Missed"/>: a safety recheck changed the support. It should stay 0: anything else means a change
    /// that reached the piece without any of the hooks seeing it, and the first <see cref="MaxWarnings"/> are logged
    /// with the piece.</item>
    /// <item><see cref="Plain"/>: no record (resting on other pieces, hanging, just loaded): the game's check as always.</item>
    /// </list>
    /// </summary>
    internal static class SupportStats
    {
        private const int MaxWarnings = 5;

        public static long Skipped;
        public static long AfterChange;
        public static long Rechecked;
        public static long Missed;
        public static long Plain;

        public static void Miss(WearNTear piece, float before)
        {
            Missed++;
            if (Missed > MaxWarnings)
            {
                return;
            }
            Vector3 at = piece.transform.position;
            HighSeatPlus.Log.LogWarning($"support recheck: {piece.name} at {at.x:0.0} {at.y:0.0} {at.z:0.0} went from " +
                $"{before:0.##} to {piece.m_support:0.##} without any change being seen; it took up to " +
                $"{SupportSkip.SafetySeconds * 1.2f:0} s to show. Please report it with the build around it.");
        }
    }
}
