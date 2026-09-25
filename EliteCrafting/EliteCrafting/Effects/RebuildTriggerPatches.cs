using HarmonyLib;

namespace EliteCrafting.Effects
{
    /// <summary>
    /// Equip, unequip and hiding or showing the hand items (swimming, eating) all end in
    /// <c>Humanoid.SetupEquipment</c>: mark the aggregate dirty when it is the local player's. Runs on every peer for
    /// every humanoid; the filter keeps it to the local player's own client.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
    internal static class EquipmentChangedPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
            {
                EffectRuntime.MarkDirty();
            }
        }
    }

    /// <summary>
    /// First spawn and respawn after death: the new Player has no status effects (death removed them all), so the
    /// aggregate is rebuilt and re-added. Local player only.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class PlayerSpawnedPatch
    {
        private static void Postfix(Player __instance)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
            {
                EffectRuntime.MarkDirty();
            }
        }
    }
}
