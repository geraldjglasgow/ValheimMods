using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Combat;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Elite
{
    /// <summary>
    /// A finished creature's attacks as Elite Creatures Reborn sees them: the item prefabs in its default items, random
    /// weapons, armour, shields, sets and random items (<see cref="CarriedItems.All"/>), where ECR looks a portal attack up
    /// by prefab name (an attack item that is in no item database is found there by its shared name, which the combat step
    /// gives every copy of its own). A
    /// definition names a portal attack by the item it knows - one the creature carries as its base did
    /// (<c>troll_throw</c>), the item a copy was made from (an attack's <c>from</c>, or a base attack the combat step
    /// copied to change it; <see cref="CreatureBuild.OriginOf"/>), or a copy's own name - and ECR gets the name the
    /// creature carries it under. Only a projectile attack goes through a portal: a melee one is warned about and left out.
    /// </summary>
    internal static class EliteAttacks
    {
        /// <summary>The line's names as the item prefab names the creature carries, its thrown attacks only.</summary>
        public static EliteLine<string>? Resolve(CreatureBuild build, EliteLine<string>? line)
        {
            if (line == null)
            {
                return null;
            }
            List<GameObject> items = Carried(build.Shell);
            List<string> kept = new List<string>();
            foreach (string name in line.Values)
            {
                foreach (GameObject item in Thrown(build, items, name, line))
                {
                    if (!kept.Contains(item.name))
                    {
                        kept.Add(item.name);
                    }
                }
            }
            return line.With(kept);
        }

        /// <summary>Whether the creature can strike at all: a weapon among its items, or its bare hands.</summary>
        public static bool HasAny(GameObject shell)
        {
            Humanoid? humanoid = shell.GetComponent<Humanoid>();
            if (humanoid != null && humanoid.m_unarmedWeapon != null)
            {
                return true;
            }
            return Carried(shell).Exists(item => Data(item)?.IsWeapon() == true);
        }

        /// <summary>The items the name means that are thrown; a warning when it means none, or none of them is thrown.</summary>
        private static List<GameObject> Thrown(CreatureBuild build, List<GameObject> items, string name, EliteLine<string> line)
        {
            List<GameObject> named = items.FindAll(item => Means(build, item, name));
            List<GameObject> thrown = named.FindAll(IsProjectile);
            if (named.Count == 0)
            {
                line.Warn($"'{name}' is not an attack it has, left out ({Hint(build, items)})");
            }
            else if (thrown.Count == 0)
            {
                line.Warn($"'{name}' is not a projectile attack, left out: only a throw or a shot goes through a portal");
            }
            return thrown;
        }

        private static bool Means(CreatureBuild build, GameObject item, string name) =>
            string.Equals(item.name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(build.OriginOf(item.name), name, StringComparison.OrdinalIgnoreCase);

        private static bool IsProjectile(GameObject item) =>
            Data(item)?.m_shared?.m_attack?.m_attackType == Attack.AttackType.Projectile;

        private static ItemDrop.ItemData? Data(GameObject item)
        {
            ItemDrop drop = item.GetComponent<ItemDrop>();
            return drop != null ? drop.m_itemData : null;
        }

        /// <summary>The names that would work, by the item each was made from where the creature carries a copy.</summary>
        private static string Hint(CreatureBuild build, List<GameObject> items)
        {
            List<string> names = new List<string>();
            foreach (GameObject item in items.FindAll(IsProjectile))
            {
                string name = build.OriginOf(item.name) ?? item.name;
                if (!names.Contains(name))
                {
                    names.Add(name);
                }
            }
            return names.Count == 0 ? "it has no projectile attack" : "its projectile attacks: " + string.Join(", ", names);
        }

        /// <summary>
        /// Its items, every list the game hands out from (<see cref="CarriedItems.All"/>, the combat step's own walk), where
        /// ECR looks a portal attack up; none for a creature that carries nothing (no Humanoid).
        /// </summary>
        private static List<GameObject> Carried(GameObject shell)
        {
            Humanoid? humanoid = shell.GetComponent<Humanoid>();
            return humanoid != null ? CarriedItems.All(humanoid) : new List<GameObject>();
        }
    }
}
