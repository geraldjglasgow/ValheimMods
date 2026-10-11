using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using EliteCreaturesPack.Custom.Humans;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// A definition's <c>attacks:</c>, each built from a weapon or creature attack item of the game or a mod (<c>from</c>,
    /// <see cref="PrefabLookup.Item"/>; anything the AI cannot attack with fails the creature).
    /// <list type="bullet">
    /// <item><b>Added</b> (the default): a new copy of the item (<see cref="OwnItems.Make"/>) joins the items the creature
    /// always carries, so it has the attack on every spawn and the AI picks it by its ranges, cooldown and preference
    /// (<c>Humanoid.EquipBestWeapon</c>). The definition's creature-wide damage and projectile go on it first
    /// (<see cref="CreatureWide"/>), then its own values.</item>
    /// <item><b>Replaced</b> (<c>replace: true</c>): the attack the creature already makes with that item changes in place,
    /// wherever it carries it (a troll's slam in its unarmed set), as its own copy; the creature-wide values are on it
    /// already. A creature that carries no such item gets it added instead, with a warning.</item>
    /// </list>
    /// A human's attack is first fitted the way its weapons are (<see cref="HumanWeaponAi.Fit"/>: a player weapon carries
    /// no AI values of its own), so the definition's own AI values are put over a fitted weapon and win. Each attack's
    /// move is checked against the moves the creature is known to make (<see cref="AttackAnimations"/>).
    /// </summary>
    internal static class NewAttacks
    {
        /// <summary>The game's AI values on an item no AI has held, which <see cref="HumanWeaponAi"/> refits as a human arms.</summary>
        private const float UnsetRange = 2f, UnsetInterval = 2f, KeptMinRange = 0.001f;

        public static void Apply(CreatureBuild build, Humanoid humanoid, CreatureWide? wide, IReadOnlyList<AttackDefinition> attacks)
        {
            HashSet<string> known = AttackAnimations.Known(build, humanoid);
            foreach (AttackDefinition attack in attacks)
            {
                if (build.Report.Failed)
                {
                    return;
                }
                GameObject? source = Source(build, attack);
                if (source == null)
                {
                    return;
                }
                foreach (GameObject item in Items(build, humanoid, wide, attack, source))
                {
                    Shape(build, known, OwnItems.Shared(item), attack);
                }
            }
        }

        /// <summary>The item named by <c>from</c>, or null after a fail.</summary>
        private static GameObject? Source(CreatureBuild build, AttackDefinition attack)
        {
            GameObject? item = build.Find.Item(attack.From);
            if (item == null)
            {
                build.Report.Fail($"unknown item '{attack.From}'", attack.Field + ".from");
                return null;
            }
            if (!CarriedItems.IsAttack(item))
            {
                build.Report.Fail($"'{attack.From}' is not a weapon or a creature attack, so no creature can attack with it", attack.Field + ".from");
                return null;
            }
            return item;
        }

        /// <summary>The creature's own items the attack is put on: the ones it replaces, or one new copy.</summary>
        private static List<GameObject> Items(CreatureBuild build, Humanoid humanoid, CreatureWide? wide, AttackDefinition attack, GameObject source)
        {
            if (attack.Replace)
            {
                List<GameObject> carried = CarriedItems.All(humanoid).Where(item => Made(build, item, attack.From)).ToList();
                if (carried.Count > 0)
                {
                    return carried.Select(item => OwnItems.Own(build, humanoid, item)).ToList();
                }
                build.Report.Warn($"it carries no '{attack.From}' to replace, so the attack is added", attack.Field + ".replace");
            }
            GameObject copy = OwnItems.Make(build, source);
            wide?.ApplyTo(copy);
            CarriedItems.Always(humanoid, new[] { copy });
            return new List<GameObject> { copy };
        }

        /// <summary>Whether a carried item is <paramref name="from"/> or a copy of it.</summary>
        private static bool Made(CreatureBuild build, GameObject item, string from) =>
            item.name == from || OwnItems.SourceOf(build, item) == from;

        private static void Shape(CreatureBuild build, HashSet<string> known, ItemDrop.ItemData.SharedData shared, AttackDefinition attack)
        {
            if (build.IsHuman)
            {
                HumanWeaponAi.Fit(shared);
            }
            AttackFields.Apply(build, shared, attack);
            if (build.IsHuman)
            {
                KeepAiValues(shared);
            }
            AttackAnimations.Check(build, known, shared, attack);
        }

        /// <summary>
        /// A human's weapons are fitted as it arms when they still carry the game's unset AI values; a definition that
        /// gives exactly those (a 2 m range every 2 s) is kept from being refitted by a minimum range too small to matter.
        /// </summary>
        private static void KeepAiValues(ItemDrop.ItemData.SharedData shared)
        {
            if (shared.m_aiAttackRange == UnsetRange && shared.m_aiAttackRangeMin == 0f && shared.m_aiAttackInterval == UnsetInterval)
            {
                shared.m_aiAttackRangeMin = KeptMinRange;
            }
        }
    }
}
