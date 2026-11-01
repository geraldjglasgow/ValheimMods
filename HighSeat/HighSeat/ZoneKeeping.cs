using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The ground under every player's area, on the server: the game builds terrain (heightmaps, colliders) only around
    /// the server's own position and lets a zone go once nobody pokes it for a few seconds. Just before that check
    /// (ZoneSystem.UpdateTTL, ten times a second) every ready player's zones are poked or built (<see cref="PlayerGround"/>,
    /// a few at a time: <see cref="TerrainAsks"/>, whose tick starts here, before the game's ghost zones), so creatures
    /// out there have ground to stand and path on; then the kept places' ground (<see cref="KeptPlaces"/>, after the
    /// players', whose ground is wanted first). With "Creatures only where shared" on a lone player's ground is still
    /// built in full: the spawners the server runs there test their spots against it (biome, height, slope, water depth)
    /// out to 80 m round the player, and a spawned creature stands on it.
    /// </summary>
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.UpdateTTL))]
    internal static class ZoneKeeping
    {
        private static void Prefix(ZoneSystem __instance)
        {
            if (!ServerRun.Active)
            {
                return;
            }
            TerrainAsks.BeginTick();
            PlayerGround.Keep(__instance);
            KeptPlaces.KeepGround(__instance);
        }
    }
}
