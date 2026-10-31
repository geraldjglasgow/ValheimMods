using System;
using HarmonyLib;
using UnityEngine;

namespace HighSeatPlus
{
    /// <summary>
    /// Heightmap.RebuildCollisionMesh (private): the ground collider of a zone was rebuilt, after a terrain edit (here
    /// or arriving from another player's TerrainComp), a zone loading or the game's regenerate. Whether a ground-held
    /// piece still touches the ground can only be told by searching again, so the columns of the whole zone are raised
    /// (<see cref="SupportCells.GroundChanged"/>). Distant terrain, which has no collider, is left out.
    /// </summary>
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.RebuildCollisionMesh))]
    internal static class SupportGroundRebuilt
    {
        private static void Postfix(Heightmap __instance)
        {
            try
            {
                Raise(__instance);
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }

        public static void Raise(Heightmap heightmap)
        {
            if (heightmap.IsDistantLod || heightmap.m_collider == null)
            {
                return;
            }
            float size = heightmap.m_width * heightmap.m_scale;
            SupportCells.GroundChanged(new Bounds(heightmap.transform.position, new Vector3(size, 1f, size)));
        }
    }

    /// <summary>Heightmap.Clear: the ground collider loses its mesh, so as for a rebuild.</summary>
    [HarmonyPatch(typeof(Heightmap), nameof(Heightmap.Clear))]
    internal static class SupportGroundCleared
    {
        private static void Postfix(Heightmap __instance)
        {
            try
            {
                SupportGroundRebuilt.Raise(__instance);
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }
    }

    /// <summary>
    /// Door.SetState (private, on every peer when a door's state reaches it): when the animator is about to swing the
    /// door, its leaf moves for about a second, through the boxes of the pieces beside it. The columns round the door
    /// (<see cref="Reach"/> past its colliders, more than a leaf is wide) are raised and settle for
    /// <see cref="SwingSeconds"/>, so no ground-held record is made while it moves and those already made are checked by
    /// the game again. Calls that change nothing (the same state, every time the door's ZDO changes) are left alone.
    /// </summary>
    [HarmonyPatch(typeof(Door), nameof(Door.SetState))]
    internal static class SupportDoorSwings
    {
        private const float Reach = 3f;
        private const float SwingSeconds = 3f;
        private static readonly int StateHash = Animator.StringToHash("state");

        private static void Prefix(Door __instance, int state)
        {
            try
            {
                Animator animator = __instance.m_animator;
                if (animator != null && animator.GetInteger(StateHash) != state &&
                    SupportBounds.Of(__instance.gameObject, out Bounds bounds))
                {
                    bounds.Expand(Reach * 2f);
                    SupportCells.Changed(bounds, SwingSeconds);
                }
            }
            catch (Exception e)
            {
                SupportSkip.Fail(e);
            }
        }
    }

    /// <summary>ZNetScene.Awake (private, a world starting): records and columns from the last world are let go.</summary>
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class SupportWorldReset
    {
        private static void Postfix()
        {
            SupportSkip.Reset();
            SupportCells.Reset();
        }
    }
}
