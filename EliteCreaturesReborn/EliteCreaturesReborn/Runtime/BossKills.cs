using System;
using System.Globalization;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// How many times the world has defeated each boss, and how far that leans its next fight's stars up (`harder each
    /// kill`). The count is a global key of the world's own, "ecr_kills_&lt;prefab&gt; N": the server keeps it in the world
    /// save and sends it to every client, so whoever owns an altar reads the same count without a message of the mod's
    /// own. The dying boss's owner records a kill by asking the server for the next count; the server ignores a line it
    /// already holds, so a Twin pair falling together counts once. The first of a Tethered pair to fall and a Phantom
    /// copy never count, nor does a boss of Hel's Trial. Counting stops at <see cref="MaxKills"/>, which also bounds the
    /// key lines a world ever sees.
    /// </summary>
    public static class BossKills
    {
        /// <summary>The most defeats that count: past this the lean stays where it is.</summary>
        public const int MaxKills = 10;

        /// <summary>Each defeat adds this to the star boost, which multiplies each star count's weight once per star
        /// (the world tier's star boost works the same way): 1.5 after one defeat, 6 after ten.</summary>
        private const float BoostPerKill = 0.5f;

        private const string Prefix = "ecr_kills_";

        /// <summary>The defeats the world holds for this boss, 0 to <see cref="MaxKills"/>.</summary>
        public static int Count(string bossPrefab)
        {
            ZoneSystem zones = ZoneSystem.instance;
            if (zones == null || string.IsNullOrEmpty(bossPrefab) || !zones.GetGlobalKey(Key(bossPrefab), out string value))
            {
                return 0;
            }
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int kills)
                ? Mathf.Clamp(kills, 0, MaxKills) : 0;
        }

        /// <summary>The star lean for this boss's next fight: 1 with `harder each kill` off or before its first defeat.</summary>
        public static float StarBoost(string bossPrefab) =>
            RuleState.Active.Boss.HarderEachKill ? 1f + BoostPerKill * Count(bossPrefab) : 1f;

        /// <summary>At a boss's death, on its owner, while its ZDO still answers. Never rethrows: nothing here may stop
        /// a creature from dying.</summary>
        public static void Record(Character victim, EliteController controller, ZDO zdo)
        {
            try
            {
                if (Counts(victim, controller.Traits, zdo))
                {
                    Write(Utils.GetPrefabName(victim.gameObject));
                }
            }
            catch (Exception e)
            {
                Guard.Report(e, "boss kill count");
            }
        }

        // An arena boss (Hel's Trial) never counts: the world's own fights are what lean the next one.
        private static bool Counts(Character victim, CreatureTraits traits, ZDO zdo) =>
            victim.IsBoss() && !traits.PhantomCopy && !Arena.Trial.ArenaTag.IsArena(zdo)
            && !(traits.HasAspect(Aspect.Tethered) && TetherPair.Standing(AspectStore.GetTether(zdo)));

        private static void Write(string bossPrefab)
        {
            int kills = Count(bossPrefab);
            if (kills < MaxKills && ZoneSystem.instance != null)
            {
                ZoneSystem.instance.SetGlobalKey($"{Key(bossPrefab)} {kills + 1}"); // lower case: the server's own spelling
            }
        }

        private static string Key(string bossPrefab) => Prefix + bossPrefab.ToLowerInvariant();
    }
}
