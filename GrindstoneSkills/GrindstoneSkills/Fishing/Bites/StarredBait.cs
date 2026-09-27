using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Starred bait keeps its stars when it comes back. Baits crafted at the prep table roll Cooking stars like dishes,
    /// and starred bait bites more (<see cref="BiteChance"/>) and hooks more big ones (<see cref="BigOne"/>). The game
    /// gives the bait back when a cast is reeled in empty (FishingFloat.ReturnBait, on the angler's client), always as a
    /// plain one; for the local player's float with starred bait the prefix gives it back with its stars instead. The
    /// bait saver gives bait back the same way (<see cref="Give"/>).
    /// </summary>
    public static class StarredBait
    {
        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.ReturnBait))]
        private static class Return
        {
            [HarmonyPrefix]
            private static bool Prefix(FishingFloat __instance)
            {
                FloatFight fight = FishSkill.Active && !__instance.m_baitConsumed ? FloatFight.Of(__instance) : null;
                Player angler = fight != null && fight.BaitStars > 0 ? Angler.LocalOf(__instance) : null;
                if (angler == null)
                    return true;
                return !HookGuard.Run("starred bait", () => Give(angler, __instance.GetBait(), fight.BaitStars), false);
            }
        }

        /// <summary>
        /// Gives the player one bait of the named prefab with these stars: into the inventory, or at their feet when it
        /// has no room (the game would lose it). False only when there is no such bait.
        /// </summary>
        public static bool Give(Player player, string bait, int stars)
        {
            GameObject prefab = string.IsNullOrEmpty(bait) || ZNetScene.instance == null ? null : ZNetScene.instance.GetPrefab(bait);
            if (player == null || prefab == null || prefab.GetComponent<ItemDrop>() == null)
                return false;
            int quality = stars > 0 ? Stars.ToQuality(stars) : 1;
            if (!player.GetInventory().CanAddItem(prefab, 1))
                DropAtFeet(player, prefab, quality);
            else if (quality > 1)
                player.GetInventory().AddItem(prefab.name, 1, quality, 0, 0L, "", false);
            else
                player.GetInventory().AddItem(prefab, 1);
            return true;
        }

        private static void DropAtFeet(Player player, GameObject prefab, int quality)
        {
            ItemDrop drop = Object.Instantiate(prefab, player.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
            ItemDrop.OnCreateNew(drop);
            drop.SetQuality(quality);
        }
    }
}
