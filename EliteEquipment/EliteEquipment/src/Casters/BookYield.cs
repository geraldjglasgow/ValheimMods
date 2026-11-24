using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// When the spellbook's held pose gives the left arm back to the game (features/casters.md, "Held open"): dead, in
    /// bed, attached (a chair, a throne, a ship's controls, a saddle), in an emote, swimming, building (the placement
    /// ghost), blocking, dodging, sitting or in a cutscene. Read on every machine from what the game already shares: the
    /// player's ZDO (dead, bed, emote, blocking), the owner's own state, and the animator every machine plays (the
    /// <c>inWater</c> and <c>attach*</c> bools the game syncs, the state tags of layer 0).
    /// </summary>
    public static class BookYield
    {
        private const string AttachPrefix = "attach";
        private static readonly int InWater = Animator.StringToHash("inWater");
        private static readonly int[] Tags =
        {
            Animator.StringToHash("sitting"), Animator.StringToHash("emote"),
            Animator.StringToHash("dodge"), Animator.StringToHash("cutscene"),
        };

        /// <summary>The animator bools that yield (swimming, every attach animation), those it has; read once per hold.</summary>
        public static int[] Bools(Animator animator)
        {
            var bools = new List<int>();
            if (animator == null || !animator.isInitialized)
                return bools.ToArray();
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Bool)
                    continue;
                if (parameter.nameHash == InWater || parameter.name.StartsWith(AttachPrefix))
                    bools.Add(parameter.nameHash);
            }
            return bools.ToArray();
        }

        /// <summary>True while the game's own use of the left arm wins.</summary>
        public static bool Now(Player player, int[] bools)
        {
            return player.IsDead() || player.InBed() || player.IsAttached() || player.InEmote() || player.IsSwimming()
                || player.InPlaceMode() || player.IsBlocking() || Animated(player.m_animator, bools);
        }

        private static bool Animated(Animator animator, int[] bools)
        {
            if (animator == null || !animator.isInitialized)
                return false;
            foreach (int hash in bools)
                if (animator.GetBool(hash))
                    return true;
            if (Tagged(animator.GetCurrentAnimatorStateInfo(0).tagHash))
                return true;
            return animator.IsInTransition(0) && Tagged(animator.GetNextAnimatorStateInfo(0).tagHash);
        }

        private static bool Tagged(int tag) => System.Array.IndexOf(Tags, tag) >= 0;
    }
}
