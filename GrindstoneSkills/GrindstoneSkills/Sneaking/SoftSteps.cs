using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Soft Steps, on the local player's own client: the noise the player makes while crouched is up to
    /// <see cref="NoiseCutAt100"/> less at Sneak 100, linear from level 0.
    /// <list type="bullet">
    /// <item>Crouched walking makes no noise in the game (Character.UpdateWalking adds it only standing), so what this
    /// quietens is the rest made from a crouch: an attack's start and hit noise (Attack), a dodge, building and removing
    /// pieces (Player), and mining (MineRock5 adds noise to the player nearest the rock, from whichever machine owns the
    /// rock). A projectile's noise where it lands (BaseAI.DoProjectileHitNoise) is the impact's, not the player's, and
    /// stays the game's.</item>
    /// <item>Crouched is the game's own test for it in Player's input (the crouch animation, or crouch toggled on), so an
    /// attack from a crouch, which plays the attack instead of the crouch animation, counts.</item>
    /// <item>Every noise ends in Character.RPC_AddNoise on the character's owner (Character.AddNoise calls it at once
    /// there, or sends it there), and a player always owns their own character. The prefix lowers the range before the
    /// game keeps it (m_noiseRange, then the status effects' ModifyNoise). Patched there rather than on AddNoise, which
    /// Pickaxes' <see cref="SplashMute"/> already prefixes: one patch per hot method.</item>
    /// <item>Creatures hear a player through Character.GetNoiseRange (BaseAI.CanHearTarget): the owner's own value here,
    /// and on every other machine the copy the owner writes to the player's ZDO twice a second. So creatures simulated
    /// anywhere, on another client or a dedicated server, hear the quieter value.</item>
    /// </list>
    /// Every other character's noise returns at the first test. Off, or at Sneak 0, every noise is the game's.
    /// </summary>
    public static class SoftSteps
    {
        private const float NoiseCutAt100 = 0.5f;

        /// <summary>The noise cut at a Sneak factor, as a share.</summary>
        public static float Cut(float factor) => NoiseCutAt100 * factor;

        [HarmonyPatch(typeof(Character), nameof(Character.RPC_AddNoise))]
        private static class Noise
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, ref float range)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer) && SneakSkill.On)
                    range = HookGuard.Run("soft steps", static noise => Quieter((Player)noise.player, noise.range),
                        (player: __instance, range), range);
            }
        }

        private static float Quieter(Player player, float range)
        {
            if (!player.m_crouchToggled && !player.IsCrouching())
                return range;
            return range * (1f - Cut(SneakSkill.Factor));
        }
    }
}
