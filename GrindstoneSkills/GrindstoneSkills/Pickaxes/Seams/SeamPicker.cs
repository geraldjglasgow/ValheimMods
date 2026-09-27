using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Chooses a seam's chunk on the miner's own client, at random among the rock's chunks that
    /// <list type="bullet">
    /// <item>still stand here (<see cref="IsStanding"/>) and were not touched by the swing that opens the seam;</item>
    /// <item>are within <see cref="Reach"/> of where the miner hit, measured to the chunk's bounds, so big chunks and
    /// small ones get the same chance;</item>
    /// <item>the miner can see: a line from the player's eye to the chunk's centre meets that chunk before anything
    /// else (another chunk, the ground), within <see cref="Sight"/>. A buried or inner chunk would be a seam nobody
    /// can strike.</item>
    /// </list>
    /// </summary>
    internal static class SeamPicker
    {
        /// <summary>How far a seam's chunk may be from the hit, in metres, to its bounds.</summary>
        public const float Reach = 2f;

        /// <summary>How far a seam's chunk centre may be from the miner's eye, in metres.</summary>
        public const float Sight = 6f;

        private static readonly List<int> Candidates = new List<int>();
        private static int sightMask;

        /// <summary>A seam's chunk near <paramref name="near"/>, not one of <paramref name="touched"/>; -1 when none fits.</summary>
        public static int Pick(Rock rock, Vector3 near, ICollection<int> touched)
        {
            Player player = Player.m_localPlayer;
            if (player == null || rock == null || !rock.IsValid)
                return -1;
            Vector3 eye = player.GetEyePoint();
            Candidates.Clear();
            int count = RockChunks.Count(rock);
            for (int area = 0; area < count; area++)
            {
                if (!touched.Contains(area) && Fits(rock, area, near, eye))
                    Candidates.Add(area);
            }
            return Candidates.Count == 0 ? -1 : Candidates[Random.Range(0, Candidates.Count)];
        }

        /// <summary>
        /// A chunk still standing on this machine: health left and its collider active. The game hides a broken
        /// chunk's collider on every machine as soon as the owner's RPC_SetAreaHealth arrives, even while this machine's
        /// other healths lag.
        /// </summary>
        public static bool IsStanding(Rock rock, int area)
        {
            Collider collider = RockChunks.ColliderOf(rock, area);
            return collider != null && collider.gameObject.activeInHierarchy && RockChunks.IsIntact(rock, area);
        }

        private static bool Fits(Rock rock, int area, Vector3 near, Vector3 eye)
        {
            if (!IsStanding(rock, area))
                return false;
            Collider collider = RockChunks.ColliderOf(rock, area);
            return collider.bounds.SqrDistance(near) <= Reach * Reach && InSight(collider, eye);
        }

        private static bool InSight(Collider collider, Vector3 eye)
        {
            Vector3 toCentre = collider.bounds.center - eye;
            float distance = toCentre.magnitude;
            if (distance < 0.01f || distance > Sight)
                return false;
            return Physics.Raycast(eye, toCentre / distance, out RaycastHit hit, distance, SightMask(), QueryTriggerInteraction.Ignore)
                && hit.collider == collider;
        }

        /// <summary>The layers the game itself tests rock chunks against (MineRock5's own mask), the ground included.</summary>
        private static int SightMask()
        {
            if (sightMask == 0)
                sightMask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
            return sightMask;
        }
    }
}
