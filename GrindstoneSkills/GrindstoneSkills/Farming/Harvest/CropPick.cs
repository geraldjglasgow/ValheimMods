using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The local player picks a crop (their use key, the scythe, a harvest piece: all go through Pickable.Interact on the
    /// picker's client). For a crop that can be picked:
    /// <list type="bullet">
    /// <item>the game's bonus crop rolls with Bonus Yield Chance At Level 100 in place of its own 25%
    /// (m_maxLevelBonusChance, on this instance, for the one call);</item>
    /// <item>its Farming experience is scaled (<see cref="FarmXp"/>: tier, giant, discovery of the crop kind);</item>
    /// <item>afterwards: "Giant turnip!" for a giant, seed return (<see cref="SeedReturn"/>) and auto-replant
    /// (<see cref="AutoReplant"/>), at the picker's level.</item>
    /// </list>
    /// The owner drops a giant's extra crop (<see cref="GiantDrop"/>). Wild crops and crops that ripened before Farming
    /// get the yield and replant perks too. Forage is the Foraging module's.
    /// </summary>
    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
    public static class CropPick
    {
        /// <summary>
        /// The pick in progress. Giant is read before the game's pick: when the picker owns the crop, the game runs
        /// RPC_Pick inside Interact and the crop's ZDO is gone by the postfix.
        /// </summary>
        public sealed class State
        {
            public Pickable Pickable;
            public CropPlant Crop;
            public float Chance;
            public bool Giant;
            public bool ScopeOpened;
            public bool Finished;
        }

        [HarmonyPrefix]
        private static void Prefix(Pickable __instance, Humanoid character, out State __state)
        {
            __state = null;
            Player player = character as Player;
            if (player == null || player != Player.m_localPlayer || !FarmSkill.Active || !CanPick(__instance))
                return;
            CropPlant crop = CropCatalog.OfPickable(__instance);
            if (crop == null)
                return;
            __state = new State
            {
                Pickable = __instance, Crop = crop, Chance = __instance.m_maxLevelBonusChance,
                Giant = CropKeys.Giant(__instance.m_nview),
            };
            __instance.m_maxLevelBonusChance = Mathf.Clamp01(FarmingPerkSettings.BonusYieldAt100.Value / 100f);
            __state.ScopeOpened = FarmXp.Begin(crop, Utils.GetPrefabName(__instance.gameObject), __state.Giant);
        }

        [HarmonyPostfix]
        private static void Postfix(Pickable __instance, Humanoid character, State __state)
        {
            if (__state == null)
                return;
            Finish(__state);
            HookGuard.Run("Farming pick", () => AfterPick(character as Player, __instance, __state));
        }

        [HarmonyFinalizer]
        private static void Finalizer(State __state)
        {
            if (__state != null)
                Finish(__state);
        }

        /// <summary>
        /// A crop the game will pick now: valid, enabled, not picked yet, not stuck in tar. Holding the use key repeats
        /// Interact every 0.2 s, and a crop another machine owns reads unpicked until its owner answers, so the game's own
        /// m_pickedLocal (set by this client's first pick) guards the extras, as it guards the game's experience.
        /// </summary>
        private static bool CanPick(Pickable pickable)
        {
            if (pickable.m_nview == null || !pickable.m_nview.IsValid() || pickable.m_picked || pickable.m_pickedLocal || !pickable.CanBePicked())
                return false;
            Floating floating = pickable.m_tarPreventsPicking ? pickable.GetComponent<Floating>() : null;
            return floating == null || !floating.IsInTar();
        }

        private static void AfterPick(Player player, Pickable pickable, State state)
        {
            if (state.Giant)
                FarmCallout.Show(pickable.transform.position, "Giant " + state.Crop.CropName().ToLowerInvariant() + "!");
            SeedReturn.Roll(player, pickable, state.Crop);
            AutoReplant.After(player, pickable, state.Crop);
        }

        /// <summary>Puts the bonus chance back and closes the experience scope, once.</summary>
        private static void Finish(State state)
        {
            if (state.Finished)
                return;
            state.Finished = true;
            if (state.Pickable != null)
                state.Pickable.m_maxLevelBonusChance = state.Chance;
            FarmXp.End(state.ScopeOpened);
        }
    }
}
