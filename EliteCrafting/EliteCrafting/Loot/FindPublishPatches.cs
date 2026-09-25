using HarmonyLib;

namespace EliteCrafting.Loot
{
    /// <summary>
    /// Equip, unequip and hiding or showing hand items all end in <c>Humanoid.SetupEquipment</c>: republish the local
    /// player's loot-find totals. Runs on every peer for every humanoid; the filter keeps it to the local player's client.
    /// </summary>
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
    internal static class FindEquipmentPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
            {
                FindPublisher.Publish();
            }
        }
    }

    /// <summary>
    /// First spawn and respawn: a respawned player is a new object with a new ZDO that holds no totals yet. Local player
    /// only.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class FindSpawnPatch
    {
        private static void Postfix(Player __instance)
        {
            if (ReferenceEquals(__instance, Player.m_localPlayer))
            {
                FindPublisher.Publish();
            }
        }
    }
}
