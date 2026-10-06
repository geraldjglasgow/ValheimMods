using System;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Patches
{
    /// <summary>
    /// Hands a Screecher the health one hit actually took from it (<see cref="ScreecherBehaviour.Hurt"/>), on the
    /// creature's owner, where the game resolves every hit; two steps of <see cref="HitPatch"/>. Before the hit it records
    /// the health, only for a Screecher on its deciding machine (anything else is told apart by its traits, with no
    /// component search), and never throws, so it can never stop a hit landing; after the hit it reads the health again,
    /// so a blocked, resisted or ignored hit counts only what got through. A failure in the shriek is reported and
    /// swallowed: the hit has landed by then and must stay landed.
    /// </summary>
    public static class ShriekDamagePatch
    {
        /// <summary>The health before the hit of a deciding Screecher; -1 for anything else.</summary>
        internal static float Capture(Struck struck)
        {
            try
            {
                EliteController? elite = struck.Elite;
                if (elite == null || !elite.Ready || !elite.Traits.Has(Mutation.Screecher))
                {
                    return -1f;
                }
                ScreecherBehaviour screecher = struck.Victim.GetComponent<ScreecherBehaviour>();
                return screecher != null && screecher.Deciding ? struck.Victim.GetHealth() : -1f;
            }
            catch (Exception e)
            {
                Guard.Report(e, "Character.RPC_Damage screecher capture");
                return -1f;
            }
        }

        internal static void Hurt(Character victim, float before)
        {
            if (before >= 0f)
            {
                SafeCall.Run("Character.RPC_Damage screecher", static (creature, health) => React(creature, health), victim,
                    before);
            }
        }

        private static void React(Character victim, float before)
        {
            ScreecherBehaviour screecher = victim.GetComponent<ScreecherBehaviour>();
            if (screecher != null)
            {
                screecher.Hurt(before - Mathf.Max(0f, victim.GetHealth()));
            }
        }
    }
}
