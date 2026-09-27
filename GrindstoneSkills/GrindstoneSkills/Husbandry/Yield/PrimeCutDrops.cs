using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Prime Cuts: meat from a starred tamed animal carries the animal's stars (level - 1, at most 3) in its quality,
    /// so Cooking treats it as a starred ingredient. "Meat" is <see cref="YieldCatalog.IsMeat"/>; it carries stars only
    /// once <see cref="YieldStarItems"/> made it a kitchen item, which happens only while Prime Cuts is on. The drops are
    /// spawned by CharacterDrop.DropItems, which instantiates each item (ItemDrop.Awake runs at once; the item saves
    /// itself to its ZDO in Start, a frame later), on one of two paths:
    /// <list type="bullet">
    /// <item>Straight from the death (a creature without a dropping ragdoll): inside Character.OnDeath, where the
    /// <see cref="Butchering"/> context holds the stars.</item>
    /// <item>From the ragdoll, a few seconds later, in Ragdoll.SpawnLoot on the ragdoll's owner (the creature's owner
    /// created it, but ownership may have moved). A postfix on Ragdoll.Setup, which runs inside Character.OnDeath,
    /// writes the stars to the ragdoll's ZDO next to the drop list the game stores there; SpawnLoot reads them back
    /// for its call.</item>
    /// </list>
    /// While either is open, an ItemDrop.Awake postfix gives each new meat item the stars, rescales it the way Awake
    /// does and saves it at once, as the cooking stations do (<see cref="StationSpawnStars"/>).
    /// </summary>
    public static class PrimeCutDrops
    {
        private static readonly int RagdollStarsHash = Keys.PrimeStars.GetStableHashCode();
        private static int ragdollStars;

        /// <summary>Stars for meat of a creature at this game level; 0 while Prime Cuts is off.</summary>
        public static int StarsFor(int level) =>
            HusbandryYieldSettings.PrimeCuts.Value && level > 1 ? Mathf.Min(level - 1, Stars.Max) : 0;

        private static int Current => Butchering.Open != null ? Butchering.Open.PrimeStars : ragdollStars;

        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Setup))]
        private static class RagdollSetup
        {
            [HarmonyPostfix]
            private static void Postfix(Ragdoll __instance, CharacterDrop characterDrop)
            {
                ButcherContext butcher = Butchering.Open;
                if (butcher != null && butcher.PrimeStars > 0 && characterDrop != null && __instance.m_dropItems)
                    HookGuard.Run("prime cuts", () => Mark(__instance, butcher.PrimeStars));
            }
        }

        [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.SpawnLoot))]
        private static class RagdollLoot
        {
            [HarmonyPrefix]
            private static void Prefix(Ragdoll __instance) =>
                ragdollStars = HusbandrySkill.Active ? HookGuard.Run("prime cuts", () => Read(__instance), 0) : 0;

            [HarmonyFinalizer]
            private static void Finalizer() => ragdollStars = 0;
        }

        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        private static class ItemAwake
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop __instance)
            {
                int stars = Current;
                if (stars > 0)
                    HookGuard.Run("prime cuts", () => Apply(__instance, stars));
            }
        }

        private static void Mark(Ragdoll ragdoll, int stars)
        {
            ZNetView nview = ragdoll.m_nview;
            if (nview != null && nview.IsValid())
                nview.GetZDO().Set(RagdollStarsHash, stars);
        }

        private static int Read(Ragdoll ragdoll)
        {
            ZNetView nview = ragdoll.m_nview;
            return nview != null && nview.IsValid() ? nview.GetZDO().GetInt(RagdollStarsHash) : 0;
        }

        private static void Apply(ItemDrop item, int stars)
        {
            ItemDrop.ItemData data = item.m_itemData;
            if (!YieldCatalog.IsMeat(data) || !Kitchen.IsKitchenItem(data))
                return;
            Stars.Set(data, stars);
            item.SetQuality(data.m_quality);
            item.Save();
        }
    }
}
