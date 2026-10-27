using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// When the sea itself is the water in a launched ship's compartment (<see cref="KwShipSeaInside"/>): from the moment
    /// its water stands within <see cref="Handover"/> of the sea outside (both a little smoothed, <see cref="Smooth"/>
    /// seconds, so a passing wave does not flick it), or while the ship is sinking (its deck under the sea for
    /// <see cref="Under"/> seconds with its hold at least <see cref="Flooded"/> full; it stops sinking after
    /// <see cref="Clear"/> seconds clear). Then the mask stops hiding the sea there and the hold's own water is not drawn
    /// (<see cref="KwShipWaterMask"/>, <see cref="KwFloodSurface"/>): the game's sea, the same water, takes over. It stays
    /// so as the ship goes down and the sea climbs over the water inside; it ends only when the compartment runs dry, or
    /// its water falls <see cref="Return"/> under the sea with a tenth or more of it gone since (bailed, or run out
    /// through the bulkheads). Plain arithmetic, the same on every screen from the same synced water.
    /// </summary>
    public sealed class KwFloodHandover
    {
        /// <summary>Metres between the water inside and the sea: the sea takes over within the first, gives back past the second.</summary>
        public const float Handover = 0.05f, Return = 0.25f;

        private const float Smooth = 3f, Under = 1f, Clear = 3f, Flooded = 0.25f, Kept = 0.9f;
        private bool[] own = new bool[0];
        private float[] gaps = new float[0], volumes = new float[0];
        private bool[] known = new bool[0];
        private float under, clear;

        /// <summary>Whether the ship is going down: every compartment is the sea's.</summary>
        public bool Sinking { get; private set; }

        /// <summary>Whether the sea is the water in compartment k.</summary>
        public bool Sea(int k) => Sinking || (k >= 0 && k < own.Length && own[k]);

        /// <summary>
        /// One step for compartment k (of <paramref name="count"/>): its water now (wet, its volume) and how far the sea
        /// stands over it (metres, <paramref name="gap"/>), <paramref name="dt"/> seconds on. True when its state changed.
        /// </summary>
        public bool Step(int k, int count, bool wet, float volume, float gap, float dt)
        {
            Size(count);
            gaps[k] = known[k] ? Mathf.Lerp(gaps[k], gap, 1f - Mathf.Exp(-dt / Smooth)) : gap;
            known[k] = wet;
            bool was = own[k];
            if (!wet)
                own[k] = false;
            else if (!own[k] && gaps[k] <= Handover)
                (own[k], volumes[k]) = (true, volume);
            else if (own[k] && gaps[k] > Return && volume < Kept * volumes[k])
                own[k] = false;
            if (own[k])
                volumes[k] = Mathf.Max(volumes[k], volume);
            return own[k] != was;
        }

        /// <summary>The ship's sinking from its deck being under the sea and how full its hold is; true when it changed.</summary>
        public bool Sink(bool deckUnder, float share, float dt)
        {
            bool was = Sinking;
            (under, clear) = deckUnder && share >= Flooded ? (under + dt, 0f) : (0f, clear + dt);
            if (!Sinking && under >= Under)
                Sinking = true;
            else if (Sinking && clear >= Clear)
                Sinking = false;
            return Sinking != was;
        }

        private void Size(int count)
        {
            if (own.Length == count)
                return;
            (own, gaps, volumes, known) = (new bool[count], new float[count], new float[count], new bool[count]);
        }
    }
}
