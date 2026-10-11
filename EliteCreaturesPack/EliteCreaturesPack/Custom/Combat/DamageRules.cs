using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// A definition's damage put on one attack's shared data (always a copy of the creature's own,
    /// <see cref="OwnItems"/>): a total first, then the types it sets outright.
    /// <list type="bullet">
    /// <item><b>Total</b>: every type of the attack scaled by one factor, so the types that hurt the living add up to the
    /// total and keep their mix. Chop and pickaxe count for trees, rocks and buildings only, so they are scaled with the
    /// rest but left out of the sum: a troll's slam (blunt 70, chop and pickaxe 100 each) at <c>total: 140</c> hits for
    /// 140 blunt and breaks things twice as hard. An attack with no damage to the living has no mix to keep: a warning,
    /// and it is left as it is.</item>
    /// <item><b>Per type</b>: each type given replaces the attack's own; the others stay.</item>
    /// </list>
    /// What the game adds on top stays the game's: stars, world level, and for a bow or crossbow the ammunition's own
    /// damage, as for players.
    /// </summary>
    internal static class DamageRules
    {
        public static void Apply(CreatureBuild build, ItemDrop.ItemData.SharedData shared, DamageBlock block, string field, string item)
        {
            if (block.Total is float total)
            {
                Scale(build, shared, total, field, item);
            }
            foreach (KeyValuePair<DamageKind, float> type in block.PerType)
            {
                Type(ref shared.m_damages, type.Key) = type.Value;
            }
        }

        private static void Scale(CreatureBuild build, ItemDrop.ItemData.SharedData shared, float total, string field, string item)
        {
            float living = shared.m_damages.GetTotalDamage() - shared.m_damages.m_chop - shared.m_damages.m_pickaxe;
            if (living <= 0f)
            {
                build.Report.Warn($"'{item}' does no damage to scale to a total of {total}: give its damage types instead", field);
                return;
            }
            shared.m_damages.Modify(total / living);
        }

        /// <summary>The field of the game's damage that a damage type of the files names.</summary>
        private static ref float Type(ref HitData.DamageTypes damages, DamageKind kind)
        {
            switch (kind)
            {
                case DamageKind.Blunt: return ref damages.m_blunt;
                case DamageKind.Slash: return ref damages.m_slash;
                case DamageKind.Pierce: return ref damages.m_pierce;
                case DamageKind.Chop: return ref damages.m_chop;
                case DamageKind.Pickaxe: return ref damages.m_pickaxe;
                case DamageKind.Fire: return ref damages.m_fire;
                case DamageKind.Frost: return ref damages.m_frost;
                case DamageKind.Lightning: return ref damages.m_lightning;
                case DamageKind.Poison: return ref damages.m_poison;
                default: return ref damages.m_spirit;
            }
        }
    }
}
