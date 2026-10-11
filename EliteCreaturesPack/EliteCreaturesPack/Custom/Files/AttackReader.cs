using System.Collections.Generic;
using EliteCreaturesPack.Custom.Definitions;
using YamlConfig;

namespace EliteCreaturesPack.Custom.Files
{
    /// <summary>Reads `attacks:`, a list of <see cref="AttackDefinition"/>.</summary>
    internal static class AttackReader
    {
        private const float MaxRange = 1000f;

        public static List<AttackDefinition>? Read(YamlNode entry, FieldReader fields)
        {
            YamlNode list = fields.At(entry, "attacks");
            if (!FieldReader.Has(list))
            {
                return null;
            }
            List<AttackDefinition> attacks = new List<AttackDefinition>();
            foreach (YamlNode item in list.Items)
            {
                fields.Note(item);
                AttackDefinition? attack = ReadAttack(item, fields);
                if (attack != null)
                {
                    attacks.Add(attack);
                }
            }
            return attacks;
        }

        private static AttackDefinition? ReadAttack(YamlNode item, FieldReader fields)
        {
            string? from = fields.Text(item, "from");
            if (from == null)
            {
                item.Error("an attack needs `from`: the weapon or creature attack item it is made from");
                return null;
            }
            AttackDefinition attack = new AttackDefinition
            {
                From = from,
                Field = fields.Relative(item),
                Replace = fields.Switch(item, "replace") ?? false,
                Animation = fields.Text(item, "animation"),
                Cooldown = fields.Number(item, "cooldown", 0f, 600f),
                Damage = DamageReader.Read(item, "damage", fields),
                StatusEffect = fields.Text(item, "status effect"),
            };
            ReadShot(item, fields, attack);
            ReadHit(item, fields, attack);
            ReadAi(item, fields, attack);
            return attack;
        }

        private static void ReadShot(YamlNode item, FieldReader fields, AttackDefinition attack)
        {
            attack.Projectile = fields.Text(item, "projectile");
            attack.Projectiles = fields.Whole(item, "projectiles", 1, 50);
            attack.Spread = fields.Number(item, "spread", 0f, 180f);
        }

        private static void ReadHit(YamlNode item, FieldReader fields, AttackDefinition attack)
        {
            attack.Reach = fields.Number(item, "reach", 0f, 100f);
            attack.Height = fields.Number(item, "height", 0f, 100f);
            attack.Width = fields.Number(item, "width", 0f, 100f);
            attack.Blockable = fields.Switch(item, "blockable");
            attack.Dodgeable = fields.Switch(item, "dodgeable");
        }

        private static void ReadAi(YamlNode item, FieldReader fields, AttackDefinition attack)
        {
            attack.MinRange = fields.Number(item, "min range", 0f, MaxRange);
            attack.MaxRange = fields.Number(item, "max range", 0f, MaxRange);
            attack.Angle = fields.Number(item, "angle", 1f, 360f);
            attack.Preferred = fields.Switch(item, "preferred");
            attack.Targets = fields.Word<AttackTarget>(item, "targets");
            if (attack.MinRange > attack.MaxRange)
            {
                item.Error($"min range {attack.MinRange} is above max range {attack.MaxRange}");
            }
        }
    }
}
