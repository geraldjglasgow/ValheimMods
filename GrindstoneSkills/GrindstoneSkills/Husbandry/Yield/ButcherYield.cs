using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Butcher Yield, on the creature's owner. CharacterDrop.GenerateDropList rolls the creature's drops into a list
    /// of (prefab, amount), already multiplied for its stars and capped at 100 each; both drop paths (the ragdoll's
    /// stored list and CharacterDrop.OnDeath's own) call it inside Character.OnDeath. A postfix, only while a
    /// <see cref="Butchering"/> context for this creature is open with a player killer, multiplies every entry that is
    /// not a trophy by 1 + the killer's share of "Butcher Yield At 100"; the fraction is a chance of one more. The
    /// list's pairs are immutable, so each scaled entry is replaced. The game's cap of 100 per entry still holds.
    /// </summary>
    public static class ButcherYield
    {
        private const int MaxAmount = 100;

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        private static class Generate
        {
            [HarmonyPostfix]
            private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
            {
                ButcherContext butcher = Butchering.Open;
                if (butcher != null && butcher.Killer != null && __result != null && __instance.m_character == butcher.Creature)
                    HookGuard.Run("butcher yield", () => Scale(__result, butcher));
            }
        }

        private static void Scale(List<KeyValuePair<GameObject, int>> drops, ButcherContext butcher)
        {
            float share = HusbandrySkill.Share(HusbandryYieldSettings.ButcherYield.Value, butcher.KillerLevel);
            if (share <= 0f)
                return;
            for (int i = 0; i < drops.Count; i++)
            {
                KeyValuePair<GameObject, int> drop = drops[i];
                if (drop.Value > 0 && Scales(drop.Key))
                    drops[i] = new KeyValuePair<GameObject, int>(drop.Key, Scaled(drop.Value, share));
            }
        }

        /// <summary>An item that is not a trophy; anything else in a drop list is left alone.</summary>
        private static bool Scales(GameObject prefab)
        {
            ItemDrop item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return item != null && item.m_itemData?.m_shared != null
                && item.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy;
        }

        /// <summary><paramref name="amount"/> times 1 + <paramref name="share"/>, the fraction a chance of one more.</summary>
        private static int Scaled(int amount, float share)
        {
            float scaled = amount * (1f + share);
            int whole = Mathf.FloorToInt(scaled);
            if (Random.value < scaled - whole)
                whole++;
            return Mathf.Clamp(whole, amount, Mathf.Max(amount, MaxAmount));
        }
    }
}
