using System;
using System.Globalization;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// The export of one creature prefab (game, mod, Elite Creatures Pack or custom) as a full definition: one entry under
    /// <c>creatures:</c> with a suggested name of its own, the prefab as its base, and every block of the schema in the
    /// order the files read them, each value its current one. Its base is the exported creature, so a line copied as it is
    /// changes nothing, and relative values (size, speed scale) are 1. What a definition cannot say is a note, with why.
    /// </summary>
    internal static class CreatureExport
    {
        public static string Text(ExportSource source, string name)
        {
            ExportWriter writer = new ExportWriter();
            WriteHeader(writer, source);
            writer.Open("creatures");
            writer.Item();
            WriteIdentity(writer, name, source.Name);
            WriteNature(writer, source);
            WriteCombat(writer, source);
            WriteLooks(writer, source);
            writer.EndItem();
            writer.Close();
            return writer.ToString();
        }

        public static void WriteIdentity(ExportWriter writer, string name, string baseName)
        {
            writer.Key("name", ExportValues.Name(name));
            writer.Key("base", ExportValues.Name(baseName));
            writer.Switch("enabled", true);
        }

        /// <summary>The blocks the character step applies: character, progress, senses, movement, behaviour, taming, sounds.</summary>
        public static void WriteNature(ExportWriter writer, ExportSource source)
        {
            CharacterExport.Write(writer, source);
            CharacterExport.WriteProgress(writer, source);
            MindExport.WriteSenses(writer, source);
            MindExport.WriteMovement(writer, source);
            MindExport.WriteBehaviour(writer, source);
            MindExport.WriteTaming(writer, source);
            MindExport.WriteSounds(writer, source);
        }

        /// <summary>The blocks of what it hits with: damage, projectile, attacks.</summary>
        public static void WriteAttackBlocks(ExportWriter writer, ExportSource source)
        {
            AttackExport.WriteDamage(writer, source);
            AttackExport.WriteProjectile(writer, source);
            AttackExport.WriteAttacks(writer, source);
        }

        /// <summary>The look step's blocks and the last two: effects, look, texture, human, elite.</summary>
        public static void WriteLooks(ExportWriter writer, ExportSource source)
        {
            LookExport.WriteEffects(writer, source);
            LookExport.WriteLook(writer, source);
            LookExport.WriteTexture(writer, source);
            HumanLookExport.Write(writer, source);
            LookExport.WriteElite(writer, source);
        }

        /// <summary>The first line of every export: what wrote it and when.</summary>
        public static void WriteStamp(ExportWriter writer, string command)
        {
            string when = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            writer.Note($"Elite Creatures Pack - `{command}`, written {when}. This file is never loaded.");
            writer.Note("");
        }

        private static void WriteCombat(ExportWriter writer, ExportSource source)
        {
            KitExport.WriteDrops(writer, source);
            KitExport.WriteGear(writer, source);
            WriteAttackBlocks(writer, source);
        }

        private static void WriteHeader(ExportWriter writer, ExportSource source)
        {
            WriteStamp(writer, "ecp export " + source.Name);
            writer.Note(source.Custom == null
                ? $"{source.Name}, a creature of the game or a mod, as a full definition: every value is its current one."
                : $"{source.Name}, a custom creature, as a full definition: every value is what its definitions built.");
            writer.Note("To make a creature from it, copy the entry under `creatures:` into EliteCreaturesPack.Creatures.yml (or "
                + "another EliteCreaturesPack.Creatures*.yml), give it a name of your own and change what you want. Its base is "
                + $"{source.Name}, so a line left as it is changes nothing and can go: what a definition leaves out keeps the "
                + "base's value. Lines starting with # are notes: values a definition cannot set, or has nothing to set, and why.");
            writer.Note("Its kit: `gear: replace: true` empties the base's gear, then gear lists what it carries and `attacks` its "
                + "attack items, each a copy with the values shown, which together give it the base's kit again.");
            writer.Note("");
        }
    }
}
