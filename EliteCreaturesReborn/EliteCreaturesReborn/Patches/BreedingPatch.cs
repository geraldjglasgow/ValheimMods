using System;
using EliteCreaturesReborn.Breeding;
using HarmonyLib;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// The game's breeding step, which runs only on the parent's owner. On a birth the newborn's traits are held for
    /// exactly this one call: a live-born young claims them as it wakes, and a laid egg that can hatch is written after
    /// the game has set it up. The finalizer lets go whatever happens, so no later spawn can inherit them.
    /// </summary>
    [HarmonyPatch(typeof(Procreation), "Procreate")]
    public static class BirthPatch
    {
        private static void Prefix(Procreation __instance) =>
            SafeCall.Run("Procreation.Procreate birth", () => Hold(__instance));

        private static void Hold(Procreation parent)
        {
            if (Parents.IsBirthNow(parent))
            {
                Lineage.Begin(() => Parents.Birth(parent), watchEggs: true);
            }
        }

        private static void Postfix() => SafeCall.Run("Procreation.Procreate egg", WriteEgg);

        private static void WriteEgg()
        {
            ItemDrop? egg = Lineage.Egg;
            Lineage.Newborn? born = egg != null && egg.GetComponent<EggGrow>() != null ? Lineage.Take() : null;
            if (egg != null && born != null)
            {
                EggData.Write(egg, born);
            }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            Lineage.End();
            return __exception;
        }
    }

    /// <summary>The moment a pregnancy starts, which is the only moment the partner is known to be beside the parent.</summary>
    [HarmonyPatch(typeof(Procreation), "MakePregnant")]
    public static class ConceptionPatch
    {
        private static void Postfix(Procreation __instance) =>
            SafeCall.Run("Procreation.MakePregnant partner", () => Parents.Conceive(__instance));
    }

    /// <summary>An item waking during a birth is the egg being laid; any other item wake is a single null check.</summary>
    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    public static class EggLaidPatch
    {
        private static void Postfix(ItemDrop __instance) => Lineage.Notice(__instance);
    }

    /// <summary>
    /// A young creature growing up: the game replaces it with a new adult, which would otherwise roll as a stranger.
    /// Its traits are held for the one call, whether it was bred or born wild, and whatever the breeding switch says.
    /// </summary>
    [HarmonyPatch(typeof(Growup), "GrowUpdate")]
    public static class GrowUpPatch
    {
        private static void Prefix(Growup __instance) =>
            SafeCall.Run("Growup.GrowUpdate traits", () => Hold(__instance));

        private static void Hold(Growup young)
        {
            Character? character = young.GetComponent<Character>();
            if (character != null && young.m_nview != null && young.m_nview.IsValid() && young.m_nview.IsOwner())
            {
                Lineage.Begin(() => Parents.Grown(character));
            }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            Lineage.End();
            return __exception;
        }
    }

    /// <summary>An egg hatching: the chick takes the traits the egg was laid with. An egg without them hatches wild.</summary>
    [HarmonyPatch(typeof(EggGrow), "GrowUpdate")]
    public static class HatchPatch
    {
        private static void Prefix(EggGrow __instance) =>
            SafeCall.Run("EggGrow.GrowUpdate traits", () => Hold(__instance));

        private static void Hold(EggGrow egg)
        {
            ItemDrop? item = egg.m_item;
            if (item != null && egg.m_nview != null && egg.m_nview.IsValid() && egg.m_nview.IsOwner()
                && EggData.Has(item.m_itemData))
            {
                Lineage.Begin(() => EggData.Read(item.m_itemData));
            }
        }

        private static Exception? Finalizer(Exception? __exception)
        {
            Lineage.End();
            return __exception;
        }
    }
}
