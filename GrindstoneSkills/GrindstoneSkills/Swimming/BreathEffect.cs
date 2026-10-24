using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The status effect behind the Breath icon (<see cref="BreathIcon"/>): it does nothing by itself, never ends on its
    /// own (no time to live) and shows the seconds of breath left as its icon text, "12s", the way the game shows a
    /// status effect's time. The text is rebuilt only when the whole second changes.
    /// </summary>
    public class BreathEffect : StatusEffect
    {
        /// <summary>Seconds of breath left, set by <see cref="Breath"/> each frame.</summary>
        public float Left;

        private int lastSeconds = -1;
        private string text = "";

        public override string GetIconText()
        {
            int seconds = Mathf.CeilToInt(Left);
            if (seconds != lastSeconds)
            {
                lastSeconds = seconds;
                text = seconds + "s";
            }
            return text;
        }
    }
}
