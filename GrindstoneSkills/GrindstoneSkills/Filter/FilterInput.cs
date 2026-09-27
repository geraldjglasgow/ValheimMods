using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Cycling the filter with the game's alternative use: Player.Interact passes alt = the AltPlace button (Left
    /// Shift unless rebound) or the gamepad's alt keys, held while Use is pressed, to the Interactable under the
    /// crosshair. Vanilla CookingStation, CraftingStation and Switch all ignore alt, so only alt presses at a kitchen
    /// are taken; normal presses stay vanilla. Held alt presses at a kitchen are swallowed, so holding the keys neither
    /// repeats the add-food switch nor cycles again. The ward check runs on the pressing client and flashes the ward,
    /// as vanilla's does; the change goes to the station's owner through <see cref="FilterRpc"/>.
    /// </summary>
    public static class FilterInput
    {
        /// <summary>Keep all, 1★ and up, 2★ and up, 3★ only, then Keep all again.</summary>
        public static int Next(int minStars) => (minStars + 1) % (Stars.Max + 1);

        /// <summary>Handles an alt press at a kitchen; the result is what Interact returns.</summary>
        private static bool Cycle(Humanoid user, ZNetView nview, bool hold)
        {
            if (hold)
                return false;
            if (!PrivateArea.CheckAccess(nview.transform.position))
                return true;
            int next = Next(KitchenFilter.MinStars(nview));
            FilterRpc.Send(nview, next);
            user.Message(MessageHud.MessageType.Center, FilterText.Changed(next));
            return true;
        }

        /// <summary>The body of an alt press's prefix: false (skip vanilla) when the press was taken.</summary>
        private static bool Take(Humanoid user, ZNetView nview, bool hold, ref bool result)
        {
            if (nview == null || user == null || user != Player.m_localPlayer)
                return true;
            result = Cycle(user, nview, hold);
            return false;
        }

        [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.Interact))]
        private static class CookingStationInteract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static bool Prefix(CookingStation __instance, Humanoid user, bool hold, bool alt, ref bool __result) =>
                !alt || Take(user, FilterStations.Of(__instance), hold, ref __result);
        }

        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
        private static class CraftingStationInteract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static bool Prefix(CraftingStation __instance, Humanoid user, bool repeat, bool alt, ref bool __result)
            {
                if (!alt || user == null || !__instance.InUseDistance(user))
                    return true;
                return Take(user, FilterStations.Of(__instance), repeat, ref __result);
            }
        }

        [HarmonyPatch(typeof(Switch), nameof(Switch.Interact))]
        private static class SwitchInteract
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.High)]
            private static bool Prefix(Switch __instance, Humanoid character, bool hold, bool alt, ref bool __result) =>
                !alt || Take(character, FilterStations.Of(__instance), hold, ref __result);
        }
    }
}
