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
        private const float RecheckSeconds = 0.25f;
        private const float MovedRecheckSeconds = 0.1f;
        private const float MovedSqr = 0.1f * 0.1f;

        private static readonly List<DepthSample> samples = new List<DepthSample>();
        private static GameObject lastGhost;
        private static bool lastWasPillar;
        private static bool failed;

        // The last verdict and what it was taken for: the position, the pillar's own id and the time. The check
        // measures the water at nine spots, so a ghost held still or a pillar looked at is checked again only after a
        // quarter second, or a tenth once it moved more than 0.1 m, or when the pillar it named is gone.
        private static PairCheck lastCheck;
        private static bool lastHadCandidate;
        private static Vector3 checkedAt;
        private static long checkedFor = -1L;
        private static float checkedTime = -10f;

        // The reason as last described, and what it was described from.
        private static string reason;
        private static string reasonToken;
        private static float reasonValue;
        private static float reasonLimit;
        private static int reasonRevision = -1;

        /// <summary>The last verdict's reason as a player reads it (<see cref="PairCheck.Describe"/>), the same string
        /// until its token, its numbers or the language change.</summary>
        internal static string Reason => reason ?? "";

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
            PairCheck check = Checked(position, selfId);
            if (check.Candidate == null)
            {
                SeaGatePreviewLine.Hide();
                SeaGatePreviewMarks.Hide();
                return check;
            }
            SeaGatePreviewLine.Show(position, check.Candidate.transform.position, check.Ok, Reason);
            if (samples.Count > 0)
                SeaGatePreviewMarks.Show(samples);
            else
                SeaGatePreviewMarks.Hide();
            return check;
        }

        /// <summary>The last verdict while it still holds for this position, else a new one.</summary>
        private static PairCheck Checked(Vector3 position, long selfId)
        {
            if (!Stale(position, selfId))
                return lastCheck;
            lastCheck = SeaGatePairRules.Check(position, selfId, samples);
            lastHadCandidate = lastCheck.Candidate != null;
            checkedAt = position;
            checkedFor = selfId;
            checkedTime = Time.time;
            Describe(lastCheck);
            return lastCheck;
        }

        private static bool Stale(Vector3 position, long selfId)
        {
            float age = Time.time - checkedTime;
            if (selfId != checkedFor || age < 0f || age >= RecheckSeconds)
                return true;
            if (lastHadCandidate && lastCheck.Candidate == null)
                return true;
            return age >= MovedRecheckSeconds && (position - checkedAt).sqrMagnitude > MovedSqr;
        }

        private static void Describe(PairCheck check)
        {
            if (reason != null && check.ReasonToken == reasonToken && check.Value == reasonValue && check.Limit == reasonLimit &&
                Language.Revision == reasonRevision)
                return;
            reason = check.Describe();
            reasonToken = check.ReasonToken;
            reasonValue = check.Value;
            reasonLimit = check.Limit;
            reasonRevision = Language.Revision;
        }

        /// <summary>The ghost while it is a shown pillar ghost in build mode, else null. Whether a ghost is a pillar is
        /// looked up once per ghost, since the game makes a new ghost whenever the selection changes.</summary>
        private static GameObject HeldPillarGhost(Player player)
        {
            GameObject ghost = player.m_placementGhost;
            if (ghost == null || !ghost.activeSelf || !player.InPlaceMode())
                return null;
            return IsPillarGhost(ghost) ? ghost : null;
        }

        /// <summary>Whether a placement ghost is a pillar, looked up once per ghost (the footing check asks too).</summary>
        internal static bool IsPillarGhost(GameObject ghost)
        {
            if (ghost != lastGhost)
            {
                lastGhost = ghost;
                lastWasPillar = SeaGatePiece.IsPillar(ghost);
            }
            return lastWasPillar;
        }
    }

    /// <summary>The one patch on the placement ghost update (<c>Player.LateUpdate</c> on the player's owner, and once
    /// more as a piece is placed): first the footing check (<see cref="SeaGateFooting"/>), which may make the placement
    /// invalid, then the preview.</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    public static class PillarGhostPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance)
        {
            SeaGateFooting.Check(__instance);
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
