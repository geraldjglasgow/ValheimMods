using EliteCreaturesReborn.Patches;
using HarmonyLib;
using Translations;

namespace EliteCreaturesReborn.Arena.Trial
{
    /// <summary>
    /// A death in Hel's hall costs no skill levels and no gear (features/arena.md section 3, "Death"): a dedicated
    /// challenge, fought until death for the most reward (the user, 2026-10-14), not a punishing death. Decided on the dying player's own machine, where the game takes the levels: a player who dies
    /// inside the built hall dies the game's own soft death - the death it gives within ten minutes of the last, which
    /// never calls <c>Skills.OnDeath</c> (the game says "The Gods are merciful") - and a world that resets skills on death
    /// does not reset them there. No tombstone is made either (<c>Player.CreateTombStone</c> skipped, as the game's own
    /// "keep inventory" world setting skips its body): the player respawns with everything they carried, still worn.
    /// The rest is the game's own death (food lost, effects cleared). Because the
    /// game's skill loss never runs, nothing that hooks it takes a level either: GrindstoneSkills' own loss (its "Skill
    /// Loss On Death", its own skills included) is applied inside <c>Skills.OnDeath</c> and so never happens, with no
    /// change on its side. After the respawn the player reads "Hel spares your skills." The same respawn pays a
    /// trial's reward that arrived while the player was dead (<see cref="Rewards.RewardGrant"/>).
    /// </summary>
    internal static class ArenaDeath
    {
        /// <summary>True inside the local player's death in the hall.</summary>
        private static bool _dying;

        /// <summary>True from a death in the hall to the next spawn of the local player.</summary>
        private static bool _spared;

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class Dying
        {
            // The death runs on the player's own machine: a time since the last death under the game's cooldown makes
            // it soft, and the game itself then lowers no skill.
            private static void Prefix(Player __instance)
            {
                ZNetView nview = __instance.m_nview;
                if (nview == null || !nview.IsValid() || !nview.IsOwner() || !ArenaHall.Built
                    || !ArenaHall.Contains(__instance.transform.position))
                {
                    return;
                }
                _dying = true;
                _spared = true;
                __instance.m_timeSinceDeath = 0f;
            }

            private static void Finalizer() => _dying = false;
        }

        /// <summary>No tombstone in the hall: everything stays in the player's inventory. Other mods' prefixes and
        /// finalizers still run (PackPanel puts its kept items back in its finalizer).</summary>
        [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
        private static class KeepGear
        {
            private static bool Prefix() => !_dying;
        }

        /// <summary>A world that resets skills on death clears them inside the death; not in the hall.</summary>
        [HarmonyPatch(typeof(Skills), nameof(Skills.Clear))]
        private static class NoReset
        {
            private static bool Prefix() => !_dying;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class Respawned
        {
            private static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }
                if (_spared)
                {
                    _spared = false;
                    SafeCall.Run("Hel spares your skills", static () => ArenaWords.Show(Lang.Get(ArenaWords.Spared)));
                }
                SafeCall.Run("Hel's Trial reward at respawn", Rewards.RewardGrant.GrantPending);
            }
        }
    }
}
