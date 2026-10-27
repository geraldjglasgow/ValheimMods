using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a mast carries besides its sails, picked on the sail window's second page (<see cref="KwSailTopsForm"/>): a crow's
    /// nest (a round railed platform near the masthead, at its height), a fighting top (a square or round platform at the
    /// lower mast's top, at its height) and a pennant streaming from a staff over the masthead (<see cref="KwPennant"/>, in
    /// the sails' cloth or a colour). The heights are the platforms' floors over the mast's foot. A mast with a platform
    /// has climbing pegs up its sides to the highest one (<see cref="KwMastClimb"/>). Saved after the sails in the mast's
    /// text, each its own part: "nest,height", "top,square,height" (or round), "pennant,colour"; a mast saved without
    /// them carries none. A fighting top lies aft of the mast, its straight side at the mast's front, clear of the square
    /// sails before it; on a mast with no square sail it lies forward instead, clear of the sail along the ship.
    /// </summary>
    public readonly struct KwMastTops
    {
        public const float MinAt = 1.5f, NestRail = 1f, TopRail = 0.9f;
        public static readonly string[] TopShapes = { "", "square", "round" };

        public KwMastTops(bool nest, float nestAt, int top, float topAt, string pennant)
        {
            (Nest, NestAt, TopAt, Pennant) = (nest, nestAt, topAt, pennant ?? "");
            Top = Mathf.Clamp(top, 0, TopShapes.Length - 1);
        }

        public bool Nest { get; }
        public float NestAt { get; }

        /// <summary>The fighting top: 0 none, 1 square, 2 round.</summary>
        public int Top { get; }
        public float TopAt { get; }

        /// <summary>The pennant's colour (<see cref="KwPennantLook"/>), "cloth" for the sails' own, empty for none.</summary>
        public string Pennant { get; }

        public bool HasPennant => !string.IsNullOrEmpty(Pennant);
        public bool HasTop => Top > 0;
        public bool Any => Nest || HasTop;

        /// <summary>The same with other values (null keeps one).</summary>
        public KwMastTops With(bool? nest = null, float? nestAt = null, int? top = null, float? topAt = null, string? pennant = null) =>
            new KwMastTops(nest ?? Nest, nestAt ?? NestAt, top ?? Top, topAt ?? TopAt, pennant ?? Pennant);

        /// <summary>The heights kept on a mast of that height: from <see cref="MinAt"/> to just under its head.</summary>
        public KwMastTops Fitted(float mast) =>
            new KwMastTops(Nest, Mathf.Clamp(NestAt, MinAt, Mathf.Max(MinAt, mast - 0.15f)), Top, Mathf.Clamp(TopAt, MinAt, Mathf.Max(MinAt, mast - 0.6f)), Pennant);

        /// <summary>The floors a climber steps between, lowest first: the deck at the mast's foot, then the top and the nest.</summary>
        public List<float> Levels()
        {
            List<float> levels = new List<float> { 0f };
            if (HasTop)
                levels.Add(TopAt);
            if (Nest)
                levels.Add(NestAt);
            levels.Sort();
            return levels;
        }

        /// <summary>The highest platform's floor (0 with none).</summary>
        public float Highest => Mathf.Max(Nest ? NestAt : 0f, HasTop ? TopAt : 0f);

        /// <summary>The top of the pennant's staff over a masthead of that height: 0.75 m over it, or over a crow's nest's rail there.</summary>
        public float StaffTop(float mast) => Mathf.Max(mast + 0.75f, Nest ? NestAt + NestRail + 0.75f : 0f);

        /// <summary>A crow's nest's radius on a mast of that height.</summary>
        public static float NestRadius(float mast) => Mathf.Clamp(0.8f + 0.03f * mast, 1f, 1.3f);

        /// <summary>A fighting top's half width across the ship (it reaches 1.2 times that along it) on a mast of that height.</summary>
        public static float TopHalf(float mast) => Mathf.Clamp(0.8f + 0.05f * mast, 1.1f, 1.7f);

        /// <summary>The way a fighting top reaches from the mast along the ship: aft (-1) on a mast with square sails, else forward (1).</summary>
        public static float TopWay(KwMastSpec mast) => mast.Squares > 0 ? -1f : 1f;

        /// <summary>The mast's own radius at a height on it (it tapers to 0.72 of its foot's at its head).</summary>
        public static float RadiusAt(float mast, float z) => KwSailLayout.RadiusOf(mast) * (1f - 0.28f * Mathf.Clamp01(z / Mathf.Max(mast, 0.1f)));

        /// <summary>The parts the mast's saved text writes for these (none for what the mast does not carry).</summary>
        public List<string> Parts()
        {
            List<string> parts = new List<string>();
            if (Nest)
                parts.Add("nest," + KwSailSpec.F(NestAt));
            if (HasTop)
                parts.Add("top," + TopShapes[Top] + "," + KwSailSpec.F(TopAt));
            if (HasPennant)
                parts.Add("pennant," + Pennant);
            return parts;
        }

        /// <summary>Reads one part of a mast's saved text into these; false when the part is not one of theirs (a sail).</summary>
        public static bool TryRead(string part, ref KwMastTops tops)
        {
            string[] p = (part ?? "").Split(',');
            if (p.Length == 2 && p[0] == "nest" && KwSailSpec.Num(p[1], out float nestAt))
                tops = tops.With(nest: true, nestAt: nestAt);
            else if (p.Length == 3 && p[0] == "top" && System.Array.IndexOf(TopShapes, p[1]) > 0 && KwSailSpec.Num(p[2], out float topAt))
                tops = tops.With(top: System.Array.IndexOf(TopShapes, p[1]), topAt: topAt);
            else if (p.Length == 2 && p[0] == "pennant" && p[1].Length > 0)
                tops = tops.With(pennant: p[1]);
            else
                return false;
            return true;
        }
    }
}
