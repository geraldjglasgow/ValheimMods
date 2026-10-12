using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// Once a frame on a machine with a HUD (never a dedicated server): the heal highlight, the turn to a
    /// locked friend, the charging bolts' little spells (every player's), the spell bar, the Spellbook.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "Update")]
    public static class CasterHud
    {
        [HarmonyPostfix]
        private static void Postfix() => CasterSafe.Run("EliteEquipment casters hud", Tick);

        private static void Tick()
        {
            if (Player.m_localPlayer == null)
                return;
            HealTarget.Tick();
            CastTurn.Tick();
            ChargeOrb.Tick();
            ChargeStream.Tick();
            WandAnimations.Tick();
            WandMoves.Tick();
            SpellBar.Tick();
            Spellbook.Tick();
        }
    }
}
