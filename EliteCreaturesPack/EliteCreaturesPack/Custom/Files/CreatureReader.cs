using System.Linq;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>
    /// Reads one entry of `creatures:` into a <see cref="CreatureDefinition"/>, block by block. Problems are reported at
    /// their node; the caller collects them and leaves the creature out when there are any.
    /// </summary>
    internal static class CreatureReader
    {
        public static CreatureDefinition Read(YamlNode entry, string file)
        {
            CreatureDefinition definition = new CreatureDefinition { File = file, Line = entry.Line };
            FieldReader fields = new FieldReader(definition, entry);
            ReadIdentity(entry, fields, definition);
            definition.Character = CharacterReader.Read(entry, fields);
            definition.Progress = CharacterReader.ReadProgress(entry, fields);
            definition.Senses = AiReader.ReadSenses(entry, fields);
            definition.Movement = AiReader.ReadMovement(entry, fields);
            definition.Behaviour = AiReader.ReadBehaviour(entry, fields);
            definition.Taming = AiReader.ReadTaming(entry, fields);
            definition.Sounds = AiReader.ReadSounds(entry, fields);
            ReadCombat(entry, fields, definition);
            definition.Effects = LookReader.ReadEffects(entry, fields);
            definition.Look = LookReader.ReadLook(entry, fields);
            definition.Texture = fields.Text(entry, "texture");
            definition.Human = HumanReader.Read(entry, fields);
            definition.Elite = EliteReader.Read(entry, fields);
            return definition;
        }

        /// <summary>The entry's name as written, for messages about an entry that could not be read; "?" when it has none.</summary>
        public static string NameOf(YamlNode entry) =>
            entry.Kind == YamlNodeKind.Map && entry.Get("name").Kind == YamlNodeKind.Scalar ? entry.Get("name").Text!.Trim() : "?";

        private static void ReadIdentity(YamlNode entry, FieldReader fields, CreatureDefinition definition)
        {
            definition.Name = fields.Text(entry, "name") ?? "";
            definition.Base = fields.Text(entry, "base") ?? "";
            definition.Enabled = fields.Switch(entry, "enabled") ?? true;
            if (definition.Name.Length == 0)
            {
                entry.Error("a creature needs a name: its prefab name");
            }
            else if (!definition.Name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
            {
                fields.At(entry, "name").Error("a name is one word of letters, digits, _ - and . (the spawn command takes it)");
            }
            if (definition.Base.Length == 0)
            {
                entry.Error("a creature needs a base: the creature it is a copy of, or Human");
            }
        }

        private static void ReadCombat(YamlNode entry, FieldReader fields, CreatureDefinition definition)
        {
            definition.Drops = LootReader.ReadDrops(entry, fields);
            definition.Gear = LootReader.ReadGear(entry, fields);
            definition.Damage = DamageReader.Read(entry, "damage", fields);
            definition.Projectile = fields.Text(entry, "projectile");
            definition.Attacks = AttackReader.Read(entry, fields);
        }
    }
}
