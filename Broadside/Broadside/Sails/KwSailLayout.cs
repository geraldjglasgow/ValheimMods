using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Where a sail's parts go, from its spec (placed on its mast, <see cref="KwMastSpec"/>), in the piece's kit axes
    /// (origin on the deck at the mast's foot, x the way a square sail bellies, y across, z up): the mast's radius and the
    /// sail's spar's (a yard thinner the higher it is, <see cref="KwSailFit.Spar"/>), the height the sail is fastened at (its
    /// yard, or its boom), where the cloth hangs from, the block its halyard runs over, and how far to each side its ropes
    /// aim (only the side counts: where they tie on is worked out from the ship's own shape, <see cref="KwRigTies"/>).
    /// </summary>
    public sealed class KwSailLayout
    {
        public KwSailLayout(KwSailSpec spec)
        {
            Spec = spec;
            Mast = spec.Mast;
            Radius = RadiusOf(Mast);
            Spar = KwSailFit.Spar(Mast, spec.Square ? spec.Tier : 0);
            At = spec.Fastened;
            Side = Mathf.Clamp(0.14f * Mast + 0.35f, 0.5f, 2.2f);
        }

        /// <summary>A mast's radius at its foot, from its height.</summary>
        public static float RadiusOf(float mast) => Mathf.Clamp(0.05f + 0.012f * mast, 0.08f, 0.26f);

        /// <summary>Every sail's layout on a mast, in the mast's order.</summary>
        public static List<KwSailLayout> Of(KwMastSpec mast)
        {
            List<KwSailLayout> sails = new List<KwSailLayout>();
            foreach (KwSailSpec sail in mast.Sails)
                sails.Add(new KwSailLayout(sail));
            return sails;
        }

        public KwSailSpec Spec { get; }
        public float Mast { get; }
        public float Radius { get; }
        public float Spar { get; }
        public float At { get; }

        /// <summary>How far to each side the shrouds, braces and sheets aim, scaled to the mast (they tie on at the ship's side, <see cref="KwRigTies"/>).</summary>
        public float Side { get; }

        public bool Square => Spec.Square;
        public float Width => Spec.Width;
        public float Height => Spec.Height;

        /// <summary>The yard's axis (square sails): in front of the mast at the fastened height.</summary>
        public float YardX => Radius + Spar;

        /// <summary>Where the cloth's origin sits: under the yard, or just aft of the mast above the boom.</summary>
        public Vector3 ClothOrigin => Square ? new Vector3(YardX, 0f, At - Spar - 0.02f) : new Vector3(-(Radius + 0.05f), 0f, At + Spar + 0.02f);

        /// <summary>
        /// The block the halyard runs over, on the mast's front for a square sail (0.23 m over its yard, under the masthead
        /// for the highest), its back for the others (just over its head).
        /// </summary>
        public Vector3 Block => new Vector3((Square ? 1f : -1f) * (Radius + 0.07f), 0f, Square ? At + 0.23f : Spec.Top + 0.08f);

        /// <summary>How much more the wind works the cloth the higher it is set: as low sails up to 4 m, a third more at 16 m.</summary>
        public float Aloft => 1f + Mathf.Clamp01((At - 4f) / 12f) / 3f;

        /// <summary>How full of wind the cloth is drawn: 1 full (the default), 0 slack, hanging with hardly a belly (<see cref="KwSailMotion"/>).</summary>
        public float Fill { get; set; } = 1f;

        /// <summary>A jib's cloth's place in its texture (<see cref="KwJibUv"/>, set by <see cref="KwJibDress.Layout"/>); none on a mast.</summary>
        public KwJibUv? Uv { get; set; }

        /// <summary>How deep a flat part of cloth h high bellies at its fullest: about a sixth of its width (or of 1.4 h), less as it empties.</summary>
        public float BellyAt(float h) => 0.17f * Mathf.Min(Width, h * 1.4f) * Mathf.Lerp(0.12f, 1f, Fill);

        /// <summary>How deep the gathered cloth's folds are at hoist f: none fully set, most furled.</summary>
        public float Bundle(float f) => (1f - f) * Mathf.Clamp(0.04f * Height + 0.12f, 0.15f, 0.4f);

        /// <summary>How deep a square sail's hauled cloth hangs in bags at hoist f: two fifths of the cloth hauled up (none
        /// of it is lost: the rest is in the bags' puff and folds), at most 3 m.</summary>
        public float Bags(float f) => Mathf.Min((1f - f) * 0.42f * Height, 3f);
    }
}
