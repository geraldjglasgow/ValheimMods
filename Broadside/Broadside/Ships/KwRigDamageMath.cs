using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// How much of a round spar's section a shot takes (<see cref="KwRigDamage"/>), plain arithmetic. A ball of half-width b
    /// passing a distance off a spar's axis cuts the band of its section within b of its path (<see cref="Band"/>); that
    /// share is drawn and kept as a notch cut from the side the shot came from, its chord at the depth that cuts the same
    /// share (<see cref="Chord"/>), scooped round like the ball along the spar (<see cref="ChordAt"/>). A spar's section lost
    /// at a place is its notches' cuts there added up (<see cref="Lost"/>); past half, it is broken through
    /// (<see cref="Breaks"/>). Chords are shares of the radius from the axis: 1 at the surface, 0 through the middle.
    /// </summary>
    public static class KwRigSection
    {
        /// <summary>The share of a section past which a spar is broken through: the lucky shot.</summary>
        public const float Breaks = 0.5f;

        /// <summary>The unit circle's area left of x (π for all of it).</summary>
        public static float Left(float x)
        {
            x = Mathf.Clamp(x, -1f, 1f);
            return Mathf.Acos(-x) + x * Mathf.Sqrt(1f - x * x);
        }

        /// <summary>The share of a circle of radius r that a band of half-width b, its middle d off the circle's, covers.</summary>
        public static float Band(float d, float b, float r)
        {
            if (r <= 0f)
                return 0f;
            float lo = (d - b) / r, hi = (d + b) / r;
            return hi <= -1f || lo >= 1f ? 0f : (Left(hi) - Left(lo)) / Mathf.PI;
        }

        /// <summary>The share of a circle beyond a chord at c (a share of the radius from the middle; -1 all of it).</summary>
        public static float Beyond(float c) => 1f - Left(c) / Mathf.PI;

        /// <summary>The chord that cuts off a share f of a circle (1 nothing, 0 half, -1 all).</summary>
        public static float Chord(float f)
        {
            float lo = -1f, hi = 1f, share = Mathf.Clamp01(f);
            for (int i = 0; i < 24; i++)
            {
                float mid = (lo + hi) / 2f;
                if (Beyond(mid) > share)
                    lo = mid;
                else
                    hi = mid;
            }
            return (lo + hi) / 2f;
        }

        /// <summary>A notch's chord a distance off its middle along the spar: c there, the surface (1) at its ends, a ball's curve between.</summary>
        public static float ChordAt(float c, float off, float half)
        {
            float q = half > 0f ? Mathf.Abs(off) / half : 1f;
            return q >= 1f ? 1f : c + (1f - c) * (1f - Mathf.Sqrt(1f - q * q));
        }

        /// <summary>The share of the section a notch (<see cref="KwRigMark.Notch"/>) cuts at a place along its spar.</summary>
        public static float Cut(in KwRigMark notch, float s) => Beyond(ChordAt(Chord(notch.C), s - notch.A, notch.D));

        /// <summary>The share of a spar's section its notches take at a place along it, at most all of it.</summary>
        public static float Lost(List<KwRigMark> notches, float s)
        {
            float lost = 0f;
            foreach (KwRigMark notch in notches)
                lost += Mathf.Abs(s - notch.A) < notch.D ? Cut(notch, s) : 0f;
            return Mathf.Min(1f, lost);
        }

        /// <summary>Where along a spar its notches take the most of its section, and how much: each middle, and between two that overlap.</summary>
        public static (float At, float Lost) Worst(List<KwRigMark> notches)
        {
            (float at, float most) = (0f, 0f);
            for (int i = 0; i < notches.Count; i++)
            {
                for (int j = i; j < notches.Count; j++)
                {
                    float s = (notches[i].A + notches[j].A) / 2f, lost = Lost(notches, s);
                    if (lost > most)
                        (at, most) = (s, lost);
                }
            }
            return (at, most);
        }
    }
}
