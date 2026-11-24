using HarmonyLib;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A spell moved by its own clip (<see cref="SpellMotion"/>) is cast by the clip's <c>Hit</c>, on the caster's machine:
    /// the first <c>Hit</c> of the cast goes to the game (its <c>DoNonAttack</c>, where <see cref="SpellCast"/> casts the
    /// spell and the wand wears once); any later one never casts the spell again, but each of Elder's Volley's next five
    /// fires its next seed (<see cref="VolleyCast.Seed"/>; ee_spell_thrust carries six <c>Hit</c>s, one a recoil), so the
    /// shots stay on the motion's recoils at any speed. A <c>Hit</c> sooner than the clip's first after the motion began
    /// is ignored: the bolt's cast state fading out as the spell fades in plays the spell's clip too, for a moment.
    /// </summary>
    [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
    public static class SpellHits
    {
        private static Attack cast;
        private static int shots;

        [HarmonyPrefix]
        private static bool Prefix(Attack __instance)
        {
            Player player = Player.m_localPlayer;
            if (player == null || __instance.m_character != player || __instance.m_attackType != Attack.AttackType.None
                || !CasterGearWorn.IsWand(__instance.m_weapon) || !SpellMotion.Drives(SpellCast.Casting))
                return true;
            if (!SpellMotion.PastFirstHit(player))
                return false;
            if (__instance != cast)
            {
                cast = __instance;
                shots = 1;
                return true;
            }
            Spell spell = SpellCast.Casting;
            if (spell.Kind == SpellKind.Strike && shots < VolleyCast.Shots && CastersSettings.On)
            {
                shots++;
                if (spell == Spells.Breath)
                    CasterSafe.Run("breath burst", () => BreathCast.Burst(player, __instance.m_weapon, spell));
                else
                    CasterSafe.Run("volley seed", () => VolleyCast.Seed(player, __instance.m_weapon, spell));
            }
            return false;
        }
    }
}
