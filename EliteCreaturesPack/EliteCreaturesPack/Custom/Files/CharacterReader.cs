using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>Reads `character:` (<see cref="CharacterBlock"/>) and `progress:` (<see cref="ProgressBlock"/>).</summary>
    internal static class CharacterReader
    {
        private const float MaxSpeed = 100f;

        public static CharacterBlock? Read(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "character");
            if (block == null)
            {
                return null;
            }
            CharacterBlock character = new CharacterBlock
            {
                DisplayName = fields.Text(block, "display name"),
                Health = fields.Number(block, "health", 1f, 1000000f),
                Faction = fields.Word<Character.Faction>(block, "faction"),
                Boss = fields.Switch(block, "boss"),
                BossFight = fields.Text(block, "boss fight"),
                Resistances = ReadResistances(block, fields),
                HurtByWater = fields.Switch(block, "hurt by water"),
                HurtByFire = fields.Switch(block, "hurt by fire"),
                HurtBySmoke = fields.Switch(block, "hurt by smoke"),
                StaggersWhenBlocked = fields.Switch(block, "staggers when blocked"),
            };
            ReadSpeeds(block, fields, character);
            return character;
        }

        public static ProgressBlock? ReadProgress(YamlNode entry, FieldReader fields)
        {
            YamlNode? block = fields.Block(entry, "progress");
            if (block == null)
            {
                return null;
            }
            return new ProgressBlock
            {
                KeyOnDeath = fields.Text(block, "key on death"),
                SpawnMessage = fields.Text(block, "spawn message"),
                DeathMessage = fields.Text(block, "death message"),
            };
        }

        private static void ReadSpeeds(YamlNode block, FieldReader fields, CharacterBlock character)
        {
            character.SpeedScale = fields.Number(block, "speed scale", 0.05f, 20f);
            character.WalkSpeed = fields.Number(block, "walk speed", 0f, MaxSpeed);
            character.Speed = fields.Number(block, "speed", 0f, MaxSpeed);
            character.RunSpeed = fields.Number(block, "run speed", 0f, MaxSpeed);
            character.TurnSpeed = fields.Number(block, "turn speed", 0f, 10000f);
            character.SwimSpeed = fields.Number(block, "swim speed", 0f, MaxSpeed);
            character.FlySpeed = fields.Number(block, "fly speed", 0f, MaxSpeed);
            character.FlyFastSpeed = fields.Number(block, "fly fast speed", 0f, MaxSpeed);
        }

        /// <summary>`resistances:` damage type to the game's modifier words; a misspelt type or word is an error.</summary>
        private static Dictionary<DamageKind, HitData.DamageModifier>? ReadResistances(YamlNode block, FieldReader fields)
        {
            YamlNode? map = fields.Block(block, "resistances");
            if (map == null)
            {
                return null;
            }
            Dictionary<DamageKind, HitData.DamageModifier> resistances = new Dictionary<DamageKind, HitData.DamageModifier>();
            foreach (KeyValuePair<string, YamlNode> entry in map.Entries)
            {
                fields.Note(entry.Value);
                DamageKind? kind = DamageReader.Kind(entry.Key, entry.Value);
                HitData.DamageModifier? modifier = fields.WordOf<HitData.DamageModifier>(entry.Value);
                if (kind != null && modifier != null)
                {
                    resistances[kind.Value] = modifier.Value;
                }
            }
            return resistances;
        }
    }
}
