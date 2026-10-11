using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Export
{
    /// <summary>
    /// `damage:`, `projectile:` and `attacks:` of an export, as the combat step applies them (Custom/Combat). Its attacks
    /// are the Humanoid's default items that are weapons the AI swings (<c>ItemData.IsWeapon</c>, what MonsterAI picks
    /// from) and that players never carry (no ObjectDB item: a creature's own attack, or a custom creature's copy), each
    /// added again as a copy of its original; and, for a custom creature, its own changed copies among the items it is
    /// given at random (lists and sets), written with <c>replace: true</c>: gear names their originals, and the replace
    /// puts the values back on the fresh copies (written before the added ones, so it reaches only those). Each is written
    /// with its item's values: animation, and for a projectile attack its projectile, count and spread,
    /// <c>m_aiAttackInterval</c> as the cooldown, <c>m_damages</c> (every type for a copy, so a type set to 0 stays 0), the
    /// hit's <c>m_attackRange</c> / <c>m_attackHeight</c> / <c>m_attackRayWidth</c>, the AI's <c>m_aiAttackRangeMin</c> /
    /// <c>m_aiAttackRange</c> / <c>m_aiAttackMaxAngle</c> / <c>m_aiPrioritized</c> / <c>m_aiTargetType</c>,
    /// <c>m_blockable</c>, <c>m_dodgeable</c> and <c>m_attackStatusEffect</c>. The creature-wide damage and projectile are
    /// written only when a custom creature's chain sets them (each attack has its own).
    /// </summary>
    internal static class AttackExport
    {
        private const float MaxRange = 1000f;

        /// <summary>Whether a carried item is an attack of the creature's own rather than gear players also carry.</summary>
        public static bool IsAttack(GameObject? item, ExportSource source)
        {
            ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
            if (drop == null || !drop.m_itemData.IsWeapon())
            {
                return false;
            }
            return source.IsPart(item!) || ObjectDB.instance == null || ObjectDB.instance.GetItemPrefab(item!.name) == null;
        }

        public static void WriteDamage(ExportWriter writer, ExportSource source)
        {
            DamageBlock? damage = source.Last(definition => definition.Damage);
            if (damage == null)
            {
                writer.Note("damage: - sets every attack's damage at once, per type or `total:` alone; each attack's own "
                    + "damage is under attacks");
                return;
            }
            writer.Open("damage");
            if (damage.Total != null)
            {
                writer.Number("total", damage.Total.Value, 0f, 1000000f);
            }
            foreach (KeyValuePair<DamageKind, float> type in damage.PerType)
            {
                writer.Number(ExportValues.Word(type.Key), type.Value, 0f, 1000000f);
            }
            writer.Close();
        }

        public static void WriteProjectile(ExportWriter writer, ExportSource source)
        {
            string? projectile = source.Last(definition => definition.Projectile);
            if (projectile == null)
            {
                writer.Note("projectile: - swaps the projectile of every attack that fires one; each attack's own is under attacks");
                return;
            }
            writer.Key("projectile", ExportValues.Name(projectile));
        }

        public static void WriteAttacks(ExportWriter writer, ExportSource source)
        {
            List<GameObject> added = KitExport.Present(source.Humanoid?.m_defaultItems).Where(item => IsAttack(item, source)).ToList();
            List<GameObject> changed = Changed(source);
            if (added.Count == 0 && changed.Count == 0)
            {
                writer.Note(source.Humanoid == null ? "attacks: none - it is not a Humanoid, so it carries no attack items"
                    : "attacks: none - it has no attack items of its own; any weapon it carries is gear, listed under gear");
                writer.Key("attacks", "[]");
                return;
            }
            writer.Note("attacks: its attack items, each a copy of the item named by `from` with these values (with replace: "
                + "the copy its gear gives it, changed)");
            writer.Open("attacks");
            changed.ForEach(item => WriteAttack(writer, item, source, replace: true));
            added.ForEach(item => WriteAttack(writer, item, source, replace: false));
            writer.Close();
        }

        /// <summary>
        /// A custom creature's own copies of weapons among what it is given at random (the weapon, armour and shield lists,
        /// the sets), one per original: gear writes their originals, which a definition gives fresh. None for a game
        /// creature, whose items are their own originals.
        /// </summary>
        private static List<GameObject> Changed(ExportSource source)
        {
            Humanoid? humanoid = source.Humanoid;
            if (humanoid == null || source.Custom == null)
            {
                return new List<GameObject>();
            }
            IEnumerable<GameObject> random = new[] { humanoid.m_randomWeapon, humanoid.m_randomArmor, humanoid.m_randomShield }
                .Concat((humanoid.m_randomSets ?? new Humanoid.ItemSet[0]).Select(set => set?.m_items))
                .SelectMany(KitExport.Present);
            return random.Where(item => source.IsPart(item) && IsAttack(item, source) && source.OriginOf(item) != null)
                .GroupBy(item => source.OriginOf(item)).Select(copies => copies.First()).ToList();
        }

        // A copy whose original is not on record is still shown, commented: a definition must name an item that exists.
        private static void WriteAttack(ExportWriter writer, GameObject item, ExportSource source, bool replace)
        {
            string? from = source.OriginOf(item);
            if (from == null)
            {
                writer.Note($"{item.name} is this creature's own copy of an attack item whose original is not on record, so "
                    + "it is left as it is");
                writer.BeginComment();
            }
            writer.Item();
            writer.Key("from", ExportValues.Name(from ?? item.name));
            if (replace)
            {
                writer.Switch("replace", true);
            }
            WriteValues(writer, item, source);
            writer.EndItem();
            if (from == null)
            {
                writer.EndComment();
            }
        }

        private static void WriteValues(ExportWriter writer, GameObject item, ExportSource source)
        {
            ItemDrop.ItemData.SharedData shared = item.GetComponent<ItemDrop>().m_itemData.m_shared;
            Attack attack = shared.m_attack;
            writer.Text("animation", attack.m_attackAnimation, "none - it plays no animation of its own");
            WriteShot(writer, attack, source);
            writer.Number("cooldown", shared.m_aiAttackInterval, 0f, 600f);
            DamageExport.WriteDamage(writer, shared.m_damages, every: source.IsPart(item));
            writer.Number("reach", attack.m_attackRange, 0f, 100f);
            writer.Number("height", attack.m_attackHeight, 0f, 100f);
            writer.Number("width", attack.m_attackRayWidth, 0f, 100f);
            WriteAi(writer, shared);
            writer.Switch("blockable", shared.m_blockable);
            writer.Switch("dodgeable", shared.m_dodgeable);
            WriteStatus(writer, shared);
        }

        /// <summary>A projectile, a count and a spread, which a definition sets on a projectile attack only (the combat
        /// step warns about them on any other).</summary>
        private static void WriteShot(ExportWriter writer, Attack attack, ExportSource source)
        {
            if (attack.m_attackType != Attack.AttackType.Projectile)
            {
                writer.Note("projectile, projectiles, spread: - for a projectile attack only");
                return;
            }
            WriteItsProjectile(writer, attack.m_attackProjectile, source);
            if (attack.m_projectiles >= 1 && attack.m_projectiles <= 50)
            {
                writer.Key("projectiles", attack.m_projectiles.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                writer.Note($"projectiles: {attack.m_projectiles} - outside the 1 to 50 a definition takes, so it is left as it is");
            }
            WriteSpread(writer, attack.m_projectileAccuracy, attack.m_projectileAccuracyMin);
        }

        // `spread` sets the stray with and without skill to one number, so two different ones cannot be written as one.
        private static void WriteSpread(ExportWriter writer, float unskilled, float skilled)
        {
            if (unskilled == skilled)
            {
                writer.Number("spread", unskilled, 0f, 180f);
                return;
            }
            writer.Note($"spread: {ExportValues.Number(unskilled)} to {ExportValues.Number(skilled)} degrees - `spread` sets "
                + "both to one number, so it is left as it is");
        }

        // A custom creature's attack fires its own copy of a projectile; the definition names the original.
        private static void WriteItsProjectile(ExportWriter writer, GameObject? projectile, ExportSource source)
        {
            if (projectile == null)
            {
                writer.Note("projectile: none - it fires nothing");
                return;
            }
            string? name = source.OriginOf(projectile);
            if (name == null || ZNetScene.instance.GetPrefab(name) == null)
            {
                writer.Note($"projectile: {projectile.name} - " + (name == null ? "its own copy, whose original is not on record"
                    : "not a prefab the game knows by name") + ", so it is left as it is");
                return;
            }
            writer.Key("projectile", ExportValues.Name(name));
        }

        private static void WriteAi(ExportWriter writer, ItemDrop.ItemData.SharedData shared)
        {
            if (shared.m_aiAttackRangeMin <= shared.m_aiAttackRange)
            {
                writer.Number("min range", shared.m_aiAttackRangeMin, 0f, MaxRange);
            }
            else
            {
                writer.Note($"min range: {ExportValues.Number(shared.m_aiAttackRangeMin)} - above its max range, which a "
                    + "definition refuses, so it is left as it is");
            }
            writer.Number("max range", shared.m_aiAttackRange, 0f, MaxRange);
            writer.Number("angle", shared.m_aiAttackMaxAngle, 1f, 360f);
            writer.Switch("preferred", shared.m_aiPrioritized);
            writer.Key("targets", ExportValues.Word(Target(shared.m_aiTargetType)));
        }

        private static AttackTarget Target(ItemDrop.ItemData.AiTarget target)
        {
            switch (target)
            {
                case ItemDrop.ItemData.AiTarget.FriendHurt: return AttackTarget.HurtFriend;
                case ItemDrop.ItemData.AiTarget.Friend: return AttackTarget.Friend;
                default: return AttackTarget.Enemy;
            }
        }

        // A definition's status effect is looked up by name in the game's list (ObjectDB), so only one there can be named;
        // and it lands on every hit, so one that lands on some hits only is left as it is (the copy keeps its own).
        private static void WriteStatus(ExportWriter writer, ItemDrop.ItemData.SharedData shared)
        {
            StatusEffect? effect = shared.m_attackStatusEffect;
            if (effect == null)
            {
                writer.Note("status effect: none");
                return;
            }
            string chance = ExportValues.Number(shared.m_attackStatusEffectChance * 100f);
            string? why = !KnownStatus(effect.name) ? "not in the game's list of status effects"
                : shared.m_attackStatusEffectChance < 1f ? $"it lands on {chance}% of its hits, a definition's on every hit"
                : null;
            if (why != null)
            {
                writer.Note($"status effect: {effect.name} - {why}, so it is left as it is");
                return;
            }
            writer.Key("status effect", ExportValues.Name(effect.name));
        }

        private static bool KnownStatus(string name) =>
            ObjectDB.instance != null && ObjectDB.instance.m_StatusEffects.Any(effect =>
                effect != null && string.Equals(effect.name, name, StringComparison.OrdinalIgnoreCase));
    }
}
