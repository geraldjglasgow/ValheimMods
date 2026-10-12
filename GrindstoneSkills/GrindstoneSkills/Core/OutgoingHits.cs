using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// The one Character.Damage prefix behind the game-skill perks that change the local player's own hits on others:
    /// Sneaking's Assassin, Dodging's Counter and the weapon milestones of Weapon Mastery. A hit is built on the
    /// attacker's client (a swing, or a projectile the shooter owns) and goes through Character.Damage there before it is
    /// sent to the target's owner, so a change made here travels with the hit and the target's owner applies it by the
    /// game's own rules. Hits on the local player, hits by anyone else and hits with no target are left alone. Each
    /// module checks its own switch and level. Defense's Riposte keeps its own prefix.
    /// </summary>
    public static class OutgoingHits
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        private static class Hit
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, HitData hit)
            {
                if (hit == null || __instance == null || LocalSkill.IsLocal(__instance))
                    return;
                Player player = Player.m_localPlayer;
                if (player != null && hit.GetAttacker() == player)
                    HookGuard.Run("outgoing hit", static args => Apply(args.target, args.hit), (target: __instance, hit));
            }
        }

        private static void Apply(Character target, HitData hit)
        {
            SneakAttack.OnHit(target, hit);
            DodgeCounter.OnHit(target, hit);
            MeleeMastery.OnHit(target, hit);
            RangedMastery.OnHit(target, hit);
        }
    }
}
