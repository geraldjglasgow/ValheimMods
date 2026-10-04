using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>While the local player holds a sea gate pillar ghost, or looks at a placed pillar that isn't paired, a
    /// line to the pillar it would pair with, green when it would pair and red with the reason and its numbers when not
    /// (the same rules the real pairing uses), and the measured water as posts and lanes (<see cref="SeaGatePreviewMarks"/>).
    /// Nothing shows when no free pillar stands within reach. Client only.</summary>
    public static class SeaGatePreview
    {
        private static readonly List<DepthSample> samples = new List<DepthSample>();
        private static GameObject lastGhost;
        private static bool lastWasPillar;
        private static bool failed;

        /// <summary>Called after the game updated the local player's placement ghost. Guarded here because the same
        /// method runs while a piece is placed: an error must never stop the placing.</summary>
        internal static void Refresh(Player player)
        {
            try
            {
                Update(player);
            }
            catch (Exception e)
            {
                SeaGatePreviewLine.Hide();
                SeaGatePreviewMarks.Hide();
                if (!failed)
                    Plugin.Log.LogError($"Sea gate preview failed (logged once): {e}");
                failed = true;
            }
        }

        private static void Update(Player player)
        {
            GameObject ghost = HeldPillarGhost(player);
            if (ghost == null)
                return;
            Draw(ghost.transform.position, 0L);
        }

        /// <summary>Checks a pillar at <paramref name="position"/> and draws the verdict for this frame; returns it.</summary>
        internal static PairCheck Draw(Vector3 position, long selfId)
        {
            PairCheck check = SeaGatePairRules.Check(position, selfId, samples);
            if (check.Candidate == null)
            {
                SeaGatePreviewLine.Hide();
                SeaGatePreviewMarks.Hide();
                return check;
            }
            SeaGatePreviewLine.Show(position, check.Candidate.transform.position, check.Ok, check.Describe());
            if (samples.Count > 0)
                SeaGatePreviewMarks.Show(samples);
            else
                SeaGatePreviewMarks.Hide();
            return check;
        }

        /// <summary>The ghost while it is a shown pillar ghost in build mode, else null. Whether a ghost is a pillar is
        /// looked up once per ghost, since the game makes a new ghost whenever the selection changes.</summary>
        private static GameObject HeldPillarGhost(Player player)
        {
            GameObject ghost = player.m_placementGhost;
            if (ghost == null || !ghost.activeSelf || !player.InPlaceMode())
                return null;
            if (ghost != lastGhost)
            {
                lastGhost = ghost;
                lastWasPillar = SeaGatePiece.IsPillar(ghost);
            }
            return lastWasPillar ? ghost : null;
        }
    }

    /// <summary>Runs the preview after the game moved the local player's placement ghost (<c>Player.LateUpdate</c> on
    /// the player's owner, and once more as a piece is placed).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class PillarGhostPreviewPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            if (__instance == null || __instance != Player.m_localPlayer)
                return;
            if (!WayfareConfig.Enabled.Value || !WayfareConfig.SeaGatesEnabled.Value)
            {
                SeaGatePreviewLine.Hide();
                return;
            }
            SeaGatePreview.Refresh(__instance);
        }
    }
}
