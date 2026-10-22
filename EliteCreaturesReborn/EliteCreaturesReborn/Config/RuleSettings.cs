using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using HarmonyLib;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// The rule file's single switches, in the .cfg so a configuration manager shows them (a player's
    /// request, 2026-10-22): the lock, the difficulty, creature stars and the mutation cap here, each mutation's on/off in
    /// <see cref="MutationSettings"/> and the respawn timers in <see cref="RespawnSettings"/>. They change the game, so
    /// <see cref="ServerLock"/> registers them with the mod's Charter ('Lock to server' is its binding switch) and the
    /// server's values hold while it binds. The parser copies them into each <see cref="RuleSet"/>; a change to any of
    /// them reads the rules again (<see cref="TakeChanged"/>). A rule file that still holds them hands them over once
    /// (<see cref="RuleMigration"/>).
    /// </summary>
    public static class RuleSettings
    {
        public const string Section = "1 - General";
        public const string ServerNote = " Set on the server: while it binds its players ('Lock to server'), the server's "
            + "value holds for everyone.";

        public static ConfigEntry<bool> LockToServer = null!;
        public static ConfigEntry<Difficulty> DifficultyChoice = null!;
        public static ConfigEntry<bool> CreatureStars = null!;
        public static ConfigEntry<int> MaxMutations = null!;

        private static bool _changed;

        /// <summary>True when this .cfg held a difficulty before this start, so a rule file's missing line means nothing.</summary>
        public static bool DifficultyWasSaved { get; private set; }

        /// <summary>The difficulty in force; the server's while it binds.</summary>
        public static Difficulty CurrentDifficulty => DifficultyChoice == null ? Difficulty.Medium : DifficultyChoice.Value;

        /// <summary>False: creatures get no stars from this mod.</summary>
        public static bool StarsOn => CreatureStars == null || CreatureStars.Value;

        /// <summary>Most mutations on one creature, 0 for no cap.</summary>
        public static int MutationCap => MaxMutations == null ? 1 : Math.Max(0, MaxMutations.Value);

        public static void Bind(ConfigFile config)
        {
            LockToServer = config.Bind(Section, "Lock to server", true,
                "Connected players use the server's rule file and the server's values of every setting marked 'Set on "
                + "the server'. The server governs this; a player's own copy is ignored while bound.");
            DifficultyWasSaved = InFile(config, Section, "Difficulty");
            DifficultyChoice = config.Bind(Section, "Difficulty", Difficulty.Medium,
                "How hard creatures get as the world's bosses fall: Easy, Medium, Hard, VeryHard or Extreme. The biome "
                + "you are entering is the gentlest; a cleared biome gets harder with every boss killed after it. Custom "
                + "uses the rule file's per-biome star chances and mutation chances with the world tier's boosts instead."
                + ServerNote);
            BindStarsAndCap(config);
            Watch(DifficultyChoice);
        }

        private static void BindStarsAndCap(ConfigFile config)
        {
            CreatureStars = config.Bind(Section, "Creature stars", true,
                "false: creatures (not bosses) get no stars from this mod and keep the stars the game or another mod "
                + "gave them; they still roll mutations, at the 0-star chance. For playing beside another star mod."
                + ServerNote);
            MaxMutations = config.Bind(Section, "Max mutations", 1, new ConfigDescription(
                "Most mutations one creature can carry. 0 = no cap. Where more roll than this allows, the ones kept are "
                + "picked at random." + ServerNote,
                new AcceptableValueRange<int>(0, MutationCatalog.InOrder.Length)));
            Watch(CreatureStars);
            Watch(MaxMutations);
        }

        /// <summary>Marks the rules for a fresh read when the entry changes: an edit, a reload, the server's push.</summary>
        public static void Watch<T>(ConfigEntry<T> entry) => entry.SettingChanged += (_, __) => _changed = true;

        /// <summary>True once after any watched setting changed; the rule reload reads the rules again.</summary>
        public static bool TakeChanged()
        {
            bool changed = _changed;
            _changed = false;
            return changed;
        }

        // BepInEx keeps a value the file holds but nothing has bound yet among its orphans.
        private static bool InFile(ConfigFile config, string section, string key)
        {
            Dictionary<ConfigDefinition, string>? orphans =
                Traverse.Create(config).Property("OrphanedEntries").GetValue<Dictionary<ConfigDefinition, string>>();
            return orphans != null && orphans.ContainsKey(new ConfigDefinition(section, key));
        }
    }
}
