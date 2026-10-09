using System.Collections.Generic;
using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// Whether this machine's player touches a trail's patches, for every kind alike: only on their feet, on the ground,
    /// not swimming, riding, sitting, in ghost mode or flying, and only within a patch's reach.
    /// </summary>
    internal static class PatchContact
    {
        /// <summary>True when <paramref name="player"/> stands where a patch could reach them.</summary>
        public static bool Feels(Player player) =>
            player.IsOnGround() && !player.IsSwimming() && !player.IsAttached() && !player.InGhostMode()
            && !player.IsDebugFlying();

        /// <summary>The greatest strength among the patches <paramref name="feet"/> stand in; false when in none.</summary>
        public static bool Strongest(Vector3 feet, List<GroundPatch> patches, out float strength)
        {
            strength = 0f;
            bool any = false;
            for (int i = 0; i < patches.Count; i++)
            {
                GroundPatch patch = patches[i];
                if (patch.Covers(feet))
                {
                    any = true;
                    strength = Mathf.Max(strength, patch.Strength);
                }
            }
            return any && strength > 0f;
        }
    }
}
