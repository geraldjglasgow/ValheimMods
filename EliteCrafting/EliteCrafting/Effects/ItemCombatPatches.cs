using System;
using HarmonyLib;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// <c>brand_damage</c> and Honing: the weapon's damage block. Honing multiplies the whole block (quality.md 3);
    /// each brand then adds its share of the item's own combat damage (every type except chop and pickaxe, measured
    /// once, after Honing, so brands never compound on each other) as its type. A slash brand also adds the same share
    /// as chop when the item chops (keen_edge on axes). A brand on a group param splits its share evenly over the
    /// group's types (judgement call: the total added stays X% of the base).
    /// <para>
    /// Runs on the attacker's client when the hit is built (Attack), so the numbers travel inside the HitData; and on
    /// any peer for the tooltip, from the same replicated item data. Hot: per hit and per tooltip frame.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), new[] { typeof(int), typeof(float) })]
    internal static class ItemDamagePatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            ItemLocalSums? sums = ItemLocalCache.Get(__instance);
            if (sums != null)
            {
                Apply(ref __result, sums);
            }
        }

        private static void Apply(ref HitData.DamageTypes damage, ItemLocalSums sums)
        {
            if (sums.RefineDamage != 1f)
            {
                damage.Modify(sums.RefineDamage);
            }
            if (!sums.HasBrand)
            {
                return;
            }
            float combat = DamageSlots.Combat(in damage);
            for (int t = 0; t < DamageSlots.Count; t++)
            {
                float share = sums.BrandShare(t);
                if (share != 0f)
                {
                    DamageSlots.AddTo(ref damage, t, combat * share);
                }
            }
            float slash = sums.BrandShare(DamageSlots.Slash);
            if (slash != 0f && damage.m_chop > 0f)
            {
                damage.m_chop += combat * slash;
            }
        }
    }

    /// <summary>
    /// <c>draw_stamina_cost</c>: stamina drained per second while this bow is held drawn. Read by the drawing player's
    /// own client (Player.UpdateAttackBowDraw). Item-local: "this bow".
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDrawStaminaDrain))]
    internal static class DrawStaminaPatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            ItemLocalSums? sums = ItemLocalCache.Get(__instance);
            if (sums != null && __result > 0f)
            {
                __result *= Math.Max(0f, 1f - sums.Get(EffectKind.DrawStaminaCost));
            }
        }
    }

    /// <summary>
    /// <c>reload_speed</c>: this crossbow reloads X% faster, i.e. the reload time is divided by <c>1 + X/100</c>
    /// (judgement call: "faster" as a rate, like move speed). Read by the reloading player's own client.
    /// </summary>
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
    internal static class ReloadSpeedPatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (!__instance.m_shared.m_attack.m_requiresReload)
            {
                return;
            }
            ItemLocalSums? sums = ItemLocalCache.Get(__instance);
            if (sums != null)
            {
                __result /= 1f + sums.Get(EffectKind.ReloadSpeed);
            }
        }
    }

    /// <summary>
    /// <c>attack_eitr_cost</c>: eitr per attack with this staff. The game checks and spends it on the attacker's own
    /// client (Attack.Start, the magic trigger feedback). Item-local: reads the weapon passed in.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), new[] { typeof(Character), typeof(ItemDrop.ItemData) })]
    internal static class AttackEitrPatch
    {
        private static void Postfix(ItemDrop.ItemData weapon, ref float __result)
        {
            ItemLocalSums? sums = ItemLocalCache.Get(weapon);
            if (sums != null && __result > 0f)
            {
                __result *= Math.Max(0f, 1f - sums.Get(EffectKind.AttackEitrCost));
            }
        }
    }
}
