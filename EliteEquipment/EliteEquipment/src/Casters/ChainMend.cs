using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Chain Mend (features/casters.md, "Nine more spells"): Mend on the friend marked as the cast started (or the caster
    /// when none is, or that friend died or went out of reach), then a jump every <see cref="Delay"/> to the nearest hurt
    /// friend within <see cref="Jump"/> of the last one healed and not healed yet, up to <see cref="Most"/> in all, each
    /// heal <see cref="Falloff"/> of the one before (<see cref="ChainMendRun"/>). Decided on the caster's machine; each heal
    /// is <c>Character.Heal</c> (the game sends it to the healed one's owner), with a Sunweave wearer's stamina as Mend's.
    /// The arc (<c>ee_fx_chainmend_arc</c>, a beam authored along +Z over 1 m, stretched between the two) and Mend's motes
    /// on each go to every player near (<see cref="CasterFx"/>'s message: the arc's start and the healed one's id).
    /// </summary>
    public static class ChainMend
    {
        public const int Most = 4;
        public const float Jump = 8f;
        public const float Falloff = 0.8f;
        public const float Delay = 0.15f;

        // A friend counts as hurt this far under their most health.
        private const float Hurt = 0.5f;
        private static readonly List<Character> near = new List<Character>();

        public static void Cast(Player caster, ItemDrop.ItemData wand, Spell spell, Character locked, Vector3 tip)
        {
            Character first = FriendTarget.StillValid(locked, caster) ? locked : caster;
            bool stamina = caster.GetSEMan().HaveStatusEffect(RobeSets.Plains.BonusHash);
            ChainMendRun.Begin(caster, spell, HealPower.Of(caster, wand), stamina, first, first == caster ? (Vector3?)null : tip);
        }

        /// <summary>One link healed, its arc from <paramref name="from"/> (none on the caster's own first) and its motes shown.</summary>
        public static void Link(Character healed, float amount, bool stamina, Vector3? from, Spell spell)
        {
            healed.Heal(amount, true);
            if (stamina)
                healed.GetSEMan().AddStatusEffect(CasterStatus.HealStaminaHash, resetTime: true);
            if (from.HasValue)
                CasterFx.Send(HealReleases.ChainArc, from.Value, healed.GetZDOID(), spell);
            CasterFx.Send(CasterFx.Mend, healed.transform.position, healed.GetZDOID(), spell);
        }

        /// <summary>The nearest hurt friend within reach of the last link, not healed yet; null for none.</summary>
        public static Character Next(Character last, Player caster, HashSet<Character> healed)
        {
            Character best = null;
            float bestDistance = Jump * Jump;
            near.Clear();
            Character.GetCharactersInRange(last.transform.position, Jump, near);
            foreach (Character character in near)
            {
                float distance = (character.transform.position - last.transform.position).sqrMagnitude;
                if (distance <= bestDistance && !healed.Contains(character) && Hurting(character, caster))
                {
                    best = character;
                    bestDistance = distance;
                }
            }
            near.Clear();
            return best;
        }

        private static bool Hurting(Character character, Player caster) =>
            FriendTarget.Friendly(character, caster) && character.GetHealth() < character.GetMaxHealth() - Hurt;

        /// <summary>The arc on this machine, from where it starts to the healed one's middle; nothing without either.</summary>
        public static void ShowArc(Vector3 from, Character to)
        {
            GameObject prefab = CasterFx.Prepared(HealReleases.ChainArc);
            if (prefab == null || to == null)
                return;
            Vector3 along = to.GetCenterPoint() - from;
            if (along.sqrMagnitude < 0.01f)
                return;
            GameObject copy = LocalEffect.Attach(prefab, null, from, endless: false);
            if (copy == null)
                return;
            copy.transform.rotation = Quaternion.LookRotation(along.normalized);
            copy.transform.localScale = new Vector3(1f, 1f, along.magnitude);
        }
    }
}
