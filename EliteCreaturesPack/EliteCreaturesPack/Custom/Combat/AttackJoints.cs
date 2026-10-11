using System.Collections.Generic;
using System.Linq;
using EliteCreaturesPack.Custom.Build;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Combat
{
    /// <summary>
    /// An attack made on one body and carried by another. An attack may come from a bone of its own body
    /// (<c>Attack.m_attackOriginJoint</c>, a troll's throwing hand), which the game looks up by name under the creature's
    /// <c>Visual</c> as the attack swings or fires, and uses unchecked: on a body without that bone every use of the attack
    /// throws, and it never lands. So every attack item the creature carries is checked against its body once it is armed:
    /// one naming a bone the body lacks becomes its own copy (<see cref="OwnItems"/>) without the bone, so it comes from the
    /// creature itself (the game's own way for an attack with no bone, its feet), with a warning; a new attack's
    /// <c>height</c> and <c>reach</c> then place it.
    /// </summary>
    internal static class AttackJoints
    {
        private const string Visual = "Visual";

        public static void Fit(CreatureBuild build, Humanoid humanoid)
        {
            Transform? body = build.Shell.transform.Find(Visual);
            if (body == null)
            {
                return;
            }
            HashSet<string> bones = new HashSet<string>(body.GetComponentsInChildren<Transform>(true).Select(bone => bone.name));
            foreach (GameObject item in CarriedItems.All(humanoid).Where(item => Missing(item, bones) != null).ToList())
            {
                string bone = Missing(item, bones)!;
                ItemDrop.ItemData.SharedData own = OwnItems.Shared(OwnItems.Own(build, humanoid, item));
                Unjoint(own.m_attack, bones);
                Unjoint(own.m_secondaryAttack, bones);
                build.Report.Warn($"'{OwnItems.SourceOf(build, item)}' attacks from the bone '{bone}', which this body lacks, so it attacks from "
                    + "the creature itself (place a new attack with `height` and `reach`)");
            }
        }

        /// <summary>The first bone the item's attacks come from that the body lacks; null when it has them all.</summary>
        private static string? Missing(GameObject item, HashSet<string> bones)
        {
            ItemDrop? drop = item.GetComponent<ItemDrop>();
            if (drop == null || drop.m_itemData.m_shared == null || !CarriedItems.IsAttack(item))
            {
                return null;
            }
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            return new[] { shared.m_attack, shared.m_secondaryAttack }
                .Select(attack => attack?.m_attackOriginJoint)
                .FirstOrDefault(joint => !string.IsNullOrEmpty(joint) && !bones.Contains(joint!));
        }

        private static void Unjoint(Attack? attack, HashSet<string> bones)
        {
            if (attack != null && !string.IsNullOrEmpty(attack.m_attackOriginJoint) && !bones.Contains(attack.m_attackOriginJoint))
            {
                attack.m_attackOriginJoint = "";
            }
        }
    }
}
