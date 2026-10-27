using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// What a launched ship's rig damage does to its sailing (<see cref="KwRigDamage"/>). The sails' push
    /// (<see cref="Push"/>, from <see cref="KwShipWind.Push"/> on the ship's owner): each sail's power by kind as
    /// <see cref="KwSailMix"/> counts it, times the share of it still working (<see cref="KwRigCondition.Working"/>: less its
    /// holes' area and the share drooping, none once its mast is down; a bowsprit's jibs none once it or their foremast is
    /// down), measured against the whole rig's power, so the push falls with the sail lost and the kinds left say how the
    /// wind gives it. A whole rig pushes exactly as the mix does. The steering (<see cref="Steer"/>, on every peer): the
    /// rudders' blade left (<see cref="KwRigBlade.Left"/>) scales the rudder's turning push, the push turning while rowing and
    /// how fast the helm swings, down to <see cref="Floor"/> of them with no blade at all, so the ship can still crawl round.
    /// </summary>
    public static class KwRigDamageDrive
    {
        /// <summary>What a ship steers with no rudder blade left, against its whole rudder.</summary>
        public const float Floor = 0.12f;

        private static readonly ConditionalWeakTable<KwRigDamage, KwRigPower> powers = new ConditionalWeakTable<KwRigDamage, KwRigPower>();
        private static readonly ConditionalWeakTable<Ship, KwRigSteering> steering = new ConditionalWeakTable<Ship, KwRigSteering>();

        /// <summary>The sails' push ahead and to leeward per unit of sail force, at an apparent wind, at full or half sail.</summary>
        public static Vector3 Push(Ship ship, KwSailMix mix, KwApparentWind air, bool halfSail)
        {
            KwRigDamage? damage = ship.GetComponent<KwRigDamage>();
            if (damage == null || damage.Condition == null || damage.Condition.Whole)
                return air.Ahead * mix.Drive(air.Angle, halfSail) + air.Leeward * mix.Side(air.Angle, halfSail);
            KwRigPower power = Power(damage);
            power.Damaged(damage);
            return air.Ahead * power.Sum(KwSailPolar.Drive, air.Angle, halfSail) + air.Leeward * power.Sum(KwSailPolar.Side, air.Angle, halfSail);
        }

        // The ship's sails' power, read again when its rig was (a sea edit, KwRigDamage.Refit).
        private static KwRigPower Power(KwRigDamage damage)
        {
            if (powers.TryGetValue(damage, out KwRigPower power) && power.Parts == damage.Parts)
                return power;
            powers.Remove(damage);
            power = KwRigPower.Of(damage.Parts);
            powers.Add(damage, power);
            return power;
        }

        /// <summary>The ship's steering scaled by its rudders' blades left (all of it when whole), from the values its build gave it.</summary>
        public static void Steer(KwRigDamage damage)
        {
            Ship? ship = damage.GetComponent<Ship>();
            if (ship == null)
                return;
            float left = 0f;
            int rudders = 0;
            foreach (KwRigPiece piece in damage.Parts.Pieces)
            {
                if (piece.Kind != KwRigPiece.RudderKind)
                    continue;
                left += KwRigBlade.Left(piece, damage.Condition.Chunks(piece.Index));
                rudders++;
            }
            float share = rudders > 0 ? Floor + (1f - Floor) * left / rudders : 1f;
            steering.GetValue(ship, s => new KwRigSteering()).Set(ship, share);
        }
    }

    /// <summary>
    /// A ship's steering as its build set it (<see cref="KwShipStats.Apply"/>), and the share of it the rudders' damage
    /// leaves (<see cref="KwRigDamageDrive.Steer"/>); taken again if anything set the ship's own values since.
    /// </summary>
    internal sealed class KwRigSteering
    {
        private float push, row, helm, share = 1f;
        private bool known;

        public void Set(Ship ship, float now)
        {
            bool moved = known && (!Mathf.Approximately(ship.m_stearVelForceFactor, push * share) || !Mathf.Approximately(ship.m_stearForce, row * share));
            if (!known || moved)
                (push, row, helm, known) = (ship.m_stearVelForceFactor, ship.m_stearForce, ship.m_rudderSpeed, true);
            share = now;
            (ship.m_stearVelForceFactor, ship.m_stearForce, ship.m_rudderSpeed) = (push * share, row * share, helm * share);
        }
    }

    /// <summary>
    /// A ship's sails' power by kind (<see cref="KwSailKind"/>), whole and as damaged, as <see cref="KwSailMix"/> counts it:
    /// each sail's area × cloth drive, forward or aft by its mast's facing, at full sail and at half (its share set there).
    /// </summary>
    internal sealed class KwRigPower
    {
        private readonly List<(int Piece, int Sail, int Kind, float Power, float Half)> sails = new List<(int, int, int, float, float)>();
        private readonly float[] wholeFull = new float[KwSailPolar.Kinds], wholeHalf = new float[KwSailPolar.Kinds];
        private readonly float[] full = new float[KwSailPolar.Kinds], half = new float[KwSailPolar.Kinds];
        private int version = -1;

        /// <summary>The rig it was read from.</summary>
        public KwRigParts Parts { get; private set; } = null!;

        public static KwRigPower Of(KwRigParts parts)
        {
            KwRigPower made = new KwRigPower { Parts = parts };
            foreach (KwRigPiece piece in parts.Pieces)
            {
                for (int i = 0; i < piece.Sails.Count && piece.Mast != null; i++)
                {
                    KwSailSpec sail = piece.Mast.Sails[i];
                    made.sails.Add((piece.Index, i, (int)KwSailPolar.Of(sail), piece.Way * sail.Power, KwShipSail.Share(piece.Mast, i, Ship.Speed.Half)));
                }
                if (piece.Kind == KwRigPiece.SpritKind && piece.JibPower > 0f)
                    made.sails.Add((piece.Index, -1, (int)KwSailKind.Jib, piece.Way * piece.JibPower, piece.JibHalf));
            }
            made.Sums(_ => 1f, made.wholeFull, made.wholeHalf);
            return made;
        }

        /// <summary>The damaged sums as the ship's marks now leave them (worked out again only when they change).</summary>
        public void Damaged(KwRigDamage damage)
        {
            if (version == damage.Version)
                return;
            version = damage.Version;
            KwRigCondition c = damage.Condition;
            Sums(s => s.Sail < 0 ? c.Jibs(damage.Parts.Get(s.Piece)!) : c.Working(s.Piece, s.Sail), full, half);
        }

        private void Sums(Func<(int Piece, int Sail, int Kind, float Power, float Half), float> working, float[] atFull, float[] atHalf)
        {
            Array.Clear(atFull, 0, atFull.Length);
            Array.Clear(atHalf, 0, atHalf.Length);
            foreach (var sail in sails)
            {
                float works = working(sail);
                atFull[sail.Kind] += sail.Power * works;
                atHalf[sail.Kind] += sail.Power * sail.Half * works;
            }
        }

        /// <summary>The kinds' values at an angle by the damaged power, against the whole rig's (as <see cref="KwSailMix"/> sums them).</summary>
        public float Sum(Func<KwSailKind, float, float> of, float angle, bool halfSail)
        {
            float[] now = halfSail ? half : full, whole = halfSail ? wholeHalf : wholeFull;
            float total = 0f, sum = 0f;
            for (int k = 0; k < now.Length; k++)
            {
                total += Mathf.Max(0f, whole[k]);
                sum += Mathf.Max(0f, now[k]) * of((KwSailKind)k, angle);
            }
            return total > 0f ? sum / total : 0f;
        }
    }
}
