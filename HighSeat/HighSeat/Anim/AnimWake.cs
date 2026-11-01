using HarmonyLib;

namespace HighSeat
{
    /// <summary>
    /// The two moments a creature the server runs needs its bones before the scan could see it (<see cref="AnimScan"/>),
    /// so it animates in full from that frame on (<see cref="AlwaysAnimate.Wake"/>):
    /// <list type="bullet">
    /// <item>An attack about to begin (before Humanoid.StartAttack sets the attack's trigger; the game's creature AI
    /// starts every attack there, and only with a target, so a try that fails wakes nothing that should sleep). A creature
    /// can find a target and attack in one AI step, and its hit comes from the attack's origin joint. The full pose is
    /// written at the animators' update later in that frame. Unity fires an update's animation events before it writes
    /// that update's pose, so a hit may read the pose of the update before; the earliest hit in a creature's attack comes
    /// 0.13 s into it (the Deathsquito's sting; most after 0.8 s), several updates on, so it reads a fresh pose.</item>
    /// <item>Damage taken (Character.ApplyDamage, which runs on the owner: hits, burning, poison, drowning, a creature's
    /// own end). A hurt creature turns, staggers, fights back or dies, and effects on a named child (a death puff on the
    /// head) are placed at its bones. A death in the same physics step as its damage (a burn's last tick) places them from
    /// the pose it had; the ragdoll spawns at the root in its own pose either way.</item>
    /// </list>
    /// Each costs one field test for anything that is not a creature (the host's own player, every player's game).
    /// </summary>
    internal static class AnimWake
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
        private static class AttackBegins
        {
            private static void Prefix(Humanoid __instance)
            {
                AlwaysAnimate.Wake(__instance);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class DamageTaken
        {
            private static void Postfix(Character __instance)
            {
                AlwaysAnimate.Wake(__instance);
            }
        }
    }
}
