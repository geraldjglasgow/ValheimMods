using System;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A ship's sails by kind (<see cref="KwSailKind"/>), from its blueprint: every mast's sails and every bowsprit's jibs,
    /// their area and their power (area × cloth drive, as <see cref="KwBpBow.Pull"/> counts it: added when the mast or
    /// bowsprit faces forward, taken away when it faces aft), at full sail and at half sail (as a launched ship sets them
    /// there, <see cref="KwShipSail.Share"/>: a stacked mast's upper square sails furled). Its drive and leeway at an
    /// apparent wind's angle are each kind's (<see cref="KwSailPolar"/>) by its share of the power set: the ship's sail
    /// force (<see cref="KwShipStats.SailFactor"/>) stays the sails' pull, and this says how much of it a wind gives. The
    /// same on every peer: pure arithmetic on the blueprint.
    /// </summary>
    public sealed class KwSailMix
    {
        public static readonly KwSailMix None = new KwSailMix();

        private readonly float[] full = new float[KwSailPolar.Kinds], half = new float[KwSailPolar.Kinds], area = new float[KwSailPolar.Kinds];

        public static KwSailMix Of(KwBlueprint blueprint)
        {
            KwSailMix mix = new KwSailMix();
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                float facing = Vector3.Dot(piece.Rotation * Vector3.forward, Vector3.forward);
                float way = Mathf.Abs(facing) < 0.1f ? 0f : Mathf.Sign(facing);
                if (piece.Prefab == KwSailTool.Made && KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast))
                    mix.Mast(mast, way);
                else if (piece.Prefab == KwBowsprit.Made)
                    mix.Jibs(blueprint, piece, way);
            }
            return mix;
        }

        /// <summary>The sail area of a kind, square metres.</summary>
        public float Area(KwSailKind kind) => area[(int)kind];

        /// <summary>The square sails' area.</summary>
        public float SquareArea => area[(int)KwSailKind.Square];

        /// <summary>The fore-and-aft sails' area: gaff and triangle sails and jibs.</summary>
        public float AlongArea => area[(int)KwSailKind.Gaff] + area[(int)KwSailKind.Triangle] + area[(int)KwSailKind.Jib];

        /// <summary>The share of the sails' power set at full sail that is square sails' (0 to 1).</summary>
        public float SquareShare => Share(full, KwSailKind.Square);

        /// <summary>The push ahead at full sail, or at half sail, with the apparent wind that far off the bow (per unit of sail force).</summary>
        public float Drive(float angle, bool halfSail) => Sum(halfSail ? half : full, KwSailPolar.Drive, angle);

        /// <summary>The push to leeward at full or half sail, as <see cref="Drive"/>.</summary>
        public float Side(float angle, bool halfSail) => Sum(halfSail ? half : full, KwSailPolar.Side, angle);

        /// <summary>Whether the ship carries a sail of that kind that pulls.</summary>
        public bool Has(KwSailKind kind) => full[(int)kind] > 0f;

        /// <summary>The most push ahead at full sail at any apparent wind's angle (every whole degree).</summary>
        public float Best
        {
            get
            {
                float best = 0f;
                for (int degrees = 0; degrees <= 180; degrees++)
                    best = Mathf.Max(best, Drive(degrees, false));
                return best;
            }
        }

        /// <summary>Sails of one kind alone (a karve's square sail, to measure against).</summary>
        public static KwSailMix Only(KwSailKind kind)
        {
            KwSailMix mix = new KwSailMix();
            mix.Add(kind, 1f, 1f, 1f, 0.5f);
            return mix;
        }

        // Each mast's sails by their shapes, each with its share set at half sail.
        private void Mast(KwMastSpec mast, float way)
        {
            for (int i = 0; i < mast.Sails.Count; i++)
            {
                KwSailSpec sail = mast.Sails[i];
                Add(KwSailPolar.Of(sail), sail.Area, sail.Power, way, KwShipSail.Share(mast, i, Ship.Speed.Half));
            }
        }

        // A bowsprit's jibs, cut to the foremast its stay comes from, set at half sail as that mast's lowest sail.
        private void Jibs(KwBlueprint blueprint, KwBpPiece piece, float way)
        {
            KwJibStay? stay = KwBowsprit.TryParse(KwShipParts.Saved(piece), out KwBowsprit spec) ? KwJibStay.InBlueprint(blueprint, piece) : null;
            if (stay == null)
                return;
            float jibs = KwJibPlan.Area(spec, stay);
            Add(KwSailKind.Jib, jibs, jibs * KwSailCloth.Drive(spec.Cloth), way, KwShipSail.Share(stay.Mast, 0, Ship.Speed.Half));
        }

        private void Add(KwSailKind kind, float sailArea, float power, float way, float halfShare)
        {
            int k = (int)kind;
            area[k] += sailArea;
            full[k] += way * power;
            half[k] += way * power * halfShare;
        }

        // The kinds' values at an angle, each by its share of the power (a kind pulling aft overall counts for none).
        private static float Sum(float[] powers, Func<KwSailKind, float, float> of, float angle)
        {
            float total = 0f, sum = 0f;
            for (int k = 0; k < powers.Length; k++)
            {
                float power = Mathf.Max(0f, powers[k]);
                total += power;
                sum += power * of((KwSailKind)k, angle);
            }
            return total > 0f ? sum / total : 0f;
        }

        private static float Share(float[] powers, KwSailKind kind)
        {
            float total = 0f;
            foreach (float power in powers)
                total += Mathf.Max(0f, power);
            return total > 0f ? Mathf.Max(0f, powers[(int)kind]) / total : 0f;
        }
    }
}
