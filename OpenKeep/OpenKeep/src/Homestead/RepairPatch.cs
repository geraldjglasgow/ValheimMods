using HarmonyLib;

namespace OpenKeep.Homestead
{
    /// <summary>
    /// <c>Player.Repair</c> (private): the hammer's repair mode, called from <c>UpdatePlacement</c> on the local
    /// player's client after the game's stamina check. The game repairs the hovered piece through
    /// <c>WearNTear.Repair</c>, which stamps the piece's <c>m_lastRepair</c> only when it sends the repair; the prefix
    /// notes that stamp and the postfix, seeing it moved, repairs the pieces touching it (<see cref="RepairArea"/>).
    /// By then the game has charged the swing (stamina, eitr, durability), played its effect and shown its message.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
    public static class RepairPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, out float __state)
        {
            WearNTear piece = RepairArea.Hovered(__instance);
            __state = piece != null ? piece.m_lastRepair : float.MaxValue;
        }

        [HarmonyPostfix]
        public static void Postfix(Player __instance, float __state)
        {
            if (!RepairSettings.AreaRepair.Value || __instance != Player.m_localPlayer)
                return;
            WearNTear piece = RepairArea.Hovered(__instance);
            if (piece != null && piece.m_lastRepair > __state)
                RepairArea.Run(__instance, piece);
        }
    }
}
