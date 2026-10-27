using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A sail's cloth at one moment (<see cref="KwSailWind.Now"/>): how full it is, how far its billows and its flapping
    /// reach (metres) and how long their waves are, how far it breathes in or out just now, which way the waves run across
    /// it, their clocks, the way the cloth bellies (its own axes) and how far it leans after the ship
    /// (<see cref="KwSailSwing"/>). The billows are long and slow (a sail's width or so, a few seconds each); the flapping
    /// shorter and quicker; both run with the wind, from the sail's leading edge to its after one and from its head to its
    /// foot, a square sail's out to both its sides with the wind right behind it. Every wave is broad and smooth over the
    /// cloth: nothing moves point by point. A point too held to move is not worked out.
    /// </summary>
    public struct KwSailNow
    {
        public float Fill, Billow, Breath, Flap;
        public float K1, K2, Kf, Way, Spill;
        public float PhaseA, PhaseB, FlapA, FlapB;
        public Vector3 Normal, Swing;

        /// <summary>Where a point of the cloth is now.</summary>
        public Vector3 At(in KwSailPoint p)
        {
            float side = Way * p.Across + Spill * p.Wide, billow = p.Belly * Billow, flap = p.Flap * Flap;
            billow = billow > 0.0005f
                ? billow * (0.6f * Mathf.Sin(K1 * (side + 0.6f * p.Down) - PhaseA) + 0.4f * Mathf.Sin(K2 * (0.4f * side + p.Down) - PhaseB))
                : 0f;
            flap = flap > 0.0005f
                ? flap * (0.65f * Mathf.Sin(Kf * (side + 0.45f * p.Down) - FlapA) + 0.35f * Mathf.Sin(1.5f * Kf * (0.5f * side + p.Down) - FlapB))
                : 0f;
            return p.Rest + p.Delta * Fill + Normal * (p.Belly * Breath + billow + flap) + Swing * p.Hang;
        }
    }
}
