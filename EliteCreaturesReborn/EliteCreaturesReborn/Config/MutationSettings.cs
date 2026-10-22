using System.Collections.Generic;
using BepInEx.Configuration;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Config
{
    /// <summary>
    /// One on/off per mutation, the rule file's old `mutations enabled` (<see cref="RuleSettings"/>). Off, no
    /// creature rolls it, whatever its chance; one that already carries it keeps it. Each entry is named by the
    /// mutation's English word and defaults to <see cref="MutationCatalog.OnByDefault"/>.
    /// </summary>
    public static class MutationSettings
    {
        public const string Section = "2 - Mutations";

        private static readonly Dictionary<Mutation, ConfigEntry<bool>> Entries = new Dictionary<Mutation, ConfigEntry<bool>>();

        // What each one does, in a few words; the compendium has the numbers.
        private static readonly Dictionary<Mutation, string> Blurbs = new Dictionary<Mutation, string>
        {
            [Mutation.Mad] = "Moves and attacks faster, with less health.",
            [Mutation.Bloated] = "More health; explodes a moment after it dies.",
            [Mutation.Cloaked] = "Invisible until you come close.",
            [Mutation.Splintering] = "Splits into weaker copies when killed, and they can split again.",
            [Mutation.Leeching] = "Regenerates when left alone and heals from the damage it deals.",
            [Mutation.Warding] = "Reflects part of each hit back at you and knocks you back.",
            [Mutation.Plated] = "Shrugs off damage at full health and hits harder as its armour goes.",
            [Mutation.Miasmic] = "Leaves poison clouds behind it.",
            [Mutation.Devouring] = "Eats smaller creatures and grows stronger; once big enough it hunts players.",
            [Mutation.Thieving] = "Steals an item with each hit; kill it to get everything back.",
            [Mutation.Gilded] = "Glitters gold, runs from players and drops extra loot and coins.",
            [Mutation.Blinking] = "Reappears behind its target during a fight, with less health.",
            [Mutation.Relentless] = "Keeps chasing you, seen or not, until you get far away.",
            [Mutation.Juggernaut] = "Never staggers or is knocked back.",
            [Mutation.Screecher] = "Shrieks when hit hard: deafens nearby players and stops Elemental and Blood Magic casting.",
            [Mutation.Frostbound] = "Slows nearby players' stamina, is healed by frost and leaves slick ice.",
            [Mutation.Mudbound] = "Leaves thick mud that slows you.",
            [Mutation.Corrodent] = "Wears your armour out faster.",
            [Mutation.Cloning] = "Leaves a harmless decoy in its place and fights on unseen.",
            [Mutation.Piercing] = "Its hits ignore part of your armour.",
            [Mutation.Howling] = "Howls to call nearby creatures of its side.",
            [Mutation.Binding] = "Leaves roots that hold you in place.",
            [Mutation.Flamebound] = "Leaves burning ground that sets you on fire.",
        };

        /// <summary>Every mutation's entry, in catalog order.</summary>
        public static IEnumerable<ConfigEntry<bool>> All => Entries.Values;

        public static void Bind(ConfigFile config)
        {
            foreach (Mutation mutation in MutationCatalog.InOrder)
            {
                ConfigEntry<bool> entry = config.Bind(Section, MutationCatalog.Word(mutation),
                    MutationCatalog.OnByDefault(mutation), Description(mutation));
                RuleSettings.Watch(entry);
                Entries[mutation] = entry;
            }
        }

        /// <summary>True when creatures may roll the mutation; the server's value while it binds.</summary>
        public static bool IsOn(Mutation mutation) =>
            Entries.TryGetValue(mutation, out ConfigEntry<bool> entry) ? entry.Value : MutationCatalog.OnByDefault(mutation);

        /// <summary>The mutation's entry, for the rule file hand-over.</summary>
        public static ConfigEntry<bool>? For(Mutation mutation) =>
            Entries.TryGetValue(mutation, out ConfigEntry<bool> entry) ? entry : null;

        private static string Description(Mutation mutation)
        {
            string blurb = Blurbs.TryGetValue(mutation, out string text) ? text + " " : "";
            return blurb + "false: no creature rolls it; one that already has it keeps it." + RuleSettings.ServerNote;
        }
    }
}
