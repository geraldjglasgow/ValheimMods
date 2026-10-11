using System;
using System.Collections.Generic;
using EliteCreaturesPack.Custom.Build;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// Whether a new attack's animation is one the creature's body can play. An attack starts by setting its animator
    /// trigger (<c>Attack.m_attackAnimation</c>, a number added for a combo or a random pick) and hits on the animation's
    /// own event: on a body with no such trigger nothing plays and the attack never lands. A prefab's animator cannot be
    /// asked for its triggers before it runs, so the moves known to work are the ones the creature's own attacks make
    /// (its base's and what earlier definitions gave it) and, for a human on the player's body, those of every player
    /// weapon. Any other is a warning, not a fail: the body may well have it.
    /// </summary>
    internal static class AttackAnimations
    {
        /// <summary>The moves known to work on the creature, read before this definition's attacks are added.</summary>
        public static HashSet<string> Known(CreatureBuild build, Humanoid humanoid)
        {
            HashSet<string> known = new HashSet<string>(StringComparer.Ordinal);
            Humanoid? root = build.Base != null ? build.Base.GetComponent<Humanoid>() : null;
            if (root != null)
            {
                AddMoves(known, CarriedItems.All(root));
            }
            AddMoves(known, CarriedItems.All(humanoid));
            if (humanoid.m_unarmedWeapon != null)
            {
                AddMoves(known, new[] { humanoid.m_unarmedWeapon.gameObject });
            }
            if (build.IsHuman && ObjectDB.instance != null)
            {
                AddMoves(known, PlayerWeapons(ObjectDB.instance));
            }
            return known;
        }

        /// <summary>Warns when the attack has no move, or one the creature is not known to make.</summary>
        public static void Check(CreatureBuild build, HashSet<string> known, ItemDrop.ItemData.SharedData shared, AttackDefinition attack)
        {
            string move = shared.m_attack.m_attackAnimation ?? "";
            string field = attack.Field + (attack.Animation != null ? ".animation" : ".from");
            if (move.Length == 0)
            {
                build.Report.Warn("the attack has no animation, so it never starts: give it one (`animation`)", field);
            }
            else if (!known.Contains(move))
            {
                string whose = build.IsHuman ? "this human's own attacks or any player weapon" : "this creature's own attacks";
                build.Report.Warn($"'{move}' is not a move {whose} make: if its body has no such animation, the attack never lands", field);
            }
        }

        private static IEnumerable<GameObject> PlayerWeapons(ObjectDB db)
        {
            foreach (GameObject item in db.m_items)
            {
                ItemDrop? drop = item != null ? item.GetComponent<ItemDrop>() : null;
                if (drop != null && drop.m_itemData.m_shared?.m_icons?.Length > 0 && drop.m_itemData.IsWeapon())
                {
                    yield return item!;
                }
            }
        }

        private static void AddMoves(HashSet<string> known, IEnumerable<GameObject> items)
        {
            foreach (GameObject item in items)
            {
                ItemDrop.ItemData.SharedData? shared = item.GetComponent<ItemDrop>()?.m_itemData.m_shared;
                AddMove(known, shared?.m_attack);
                AddMove(known, shared?.m_secondaryAttack);
            }
        }

        private static void AddMove(HashSet<string> known, Attack? attack)
        {
            if (attack != null && !string.IsNullOrEmpty(attack.m_attackAnimation))
            {
                known.Add(attack.m_attackAnimation);
            }
        }
    }
}
