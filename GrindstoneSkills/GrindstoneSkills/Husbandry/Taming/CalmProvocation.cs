using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Breaking the calm: a creature being tamed that a player hurts stays wary of that player for Calm Break Time, so
    /// it defends itself as the game would. The game tells a creature's AI about damage on its owner
    /// (<c>Character.m_onDamaged</c> calls <c>MonsterAI.OnDamaged</c>, which alerts it and targets the attacker). A
    /// postfix there writes the attacker's player ID and the time to the creature's ZDO (<see cref="Keys.CalmBrokenBy"/>,
    /// <see cref="Keys.CalmBrokenAt"/>); <see cref="Calm"/> reads them, so the target the game just set is kept. Only the
    /// last player who hurt it is remembered.
    /// </summary>
    public static class CalmProvocation
    {
        [HarmonyPatch(typeof(MonsterAI), nameof(MonsterAI.OnDamaged))]
        private static class Hurt
        {
            [HarmonyPostfix]
            private static void Postfix(MonsterAI __instance, Character attacker) =>
                Guard.Run("calm provocation", () => Remember(__instance, attacker));
        }

        private static void Remember(MonsterAI ai, Character attacker)
        {
            if (!(attacker is Player player) || ai.m_character == null || ai.m_character.IsTamed())
                return;
            ZNetView nview = ai.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || ai.m_tamable == null)
                return;
            nview.GetZDO().Set(Keys.CalmBrokenBy, player.GetPlayerID());
            nview.GetZDO().Set(Keys.CalmBrokenAt, Herd.Now.Ticks);
        }

        /// <summary>True while <paramref name="tameable"/> is wary of <paramref name="player"/> after being hurt by them.</summary>
        public static bool IsWary(Tameable tameable, Player player)
        {
            ZDO zdo = tameable.m_nview.GetZDO();
            long by = zdo.GetLong(Keys.CalmBrokenBy);
            if (by == 0L || by != player.GetPlayerID())
                return false;
            return Herd.SecondsSince(zdo.GetLong(Keys.CalmBrokenAt)) < HusbandryTamingSettings.CalmBreakTime.Value;
        }
    }
}
