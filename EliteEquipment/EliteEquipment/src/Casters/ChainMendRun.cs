using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One Chain Mend on the caster's machine (<see cref="ChainMend"/>): heals its first link at once, then jumps every
    /// <see cref="ChainMend.Delay"/> until <see cref="ChainMend.Most"/> are healed, no hurt friend is left in reach, or the
    /// caster leaves the game; each heal <see cref="ChainMend.Falloff"/> of the one before.
    /// </summary>
    public sealed class ChainMendRun : MonoBehaviour
    {
        private readonly HashSet<Character> healed = new HashSet<Character>();
        private Player caster;
        private Spell spell;
        private float amount;
        private bool stamina;
        private Character last;
        private float next;

        /// <summary>A chain begun at <paramref name="first"/>, its arc from <paramref name="from"/> (the tip; none for the caster).</summary>
        public static void Begin(Player caster, Spell spell, float amount, bool stamina, Character first, Vector3? from)
        {
            var go = new GameObject("EE_ChainMend");
            ChainMendRun run = go.AddComponent<ChainMendRun>();
            run.caster = caster;
            run.spell = spell;
            run.amount = amount;
            run.stamina = stamina;
            run.Hop(first, from);
        }

        private void Update()
        {
            if (Time.time < next)
                return;
            Character to = caster != null && last != null && healed.Count < ChainMend.Most && CastersSettings.On
                ? ChainMend.Next(last, caster, healed)
                : null;
            if (to == null)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 from = last.GetCenterPoint();
            CasterSafe.Run("chain mend", () => Hop(to, from));
        }

        private void Hop(Character to, Vector3? from)
        {
            healed.Add(to);
            last = to;
            next = Time.time + ChainMend.Delay;
            ChainMend.Link(to, amount, stamina, from, spell);
            amount *= ChainMend.Falloff;
        }
    }
}
