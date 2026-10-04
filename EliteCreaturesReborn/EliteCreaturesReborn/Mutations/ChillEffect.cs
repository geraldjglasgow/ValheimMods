using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// The live "Chilled" status effect on this machine's own player: the game's stat effect (so the game itself slows
    /// the stamina regeneration and the HUD lists it with its stat line), held rather than timed. Each aura check in reach
    /// holds it a moment longer, and it ends a moment after the last one, so it shows no countdown while it lasts and
    /// needs nobody to take it off - not even a creature that died or unloaded with the player beside it. Two auras never
    /// stack: the strongest cut in reach wins, and a weaker one takes over only once the stronger has stopped holding it.
    /// </summary>
    public sealed class ChillEffect : SE_Stats
    {
        private float _cut;
        private float _strongUntil;
        private float _heldUntil;

        /// <summary>
        /// Keeps the chill on for <paramref name="seconds"/> more, cutting stamina regeneration by <paramref name="cut"/>
        /// percent - or by the stronger cut already held, while that one is still being held.
        /// </summary>
        public void Hold(float cut, float seconds)
        {
            float now = Time.time;
            if (cut >= _cut || now > _strongUntil)
            {
                _cut = cut;
                _strongUntil = now + seconds;
            }
            _heldUntil = now + seconds;
            m_staminaRegenMultiplier = 1f - Mathf.Clamp(_cut, 0f, 100f) / 100f;
        }

        /// <summary>Done once nothing has held it for its hold time; the game then removes it like any other effect.</summary>
        public override bool IsDone() => Time.time > _heldUntil;
    }
}
