using System.Collections.Generic;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>The pairing rules, the same for the placement preview and the real pairing: the nearest free pillar
    /// (preferring one at a width and height that pair), then footing (land or shallow water), width, height, the water between the pillars and
    /// the sides a ship may come out on, in that order.
    /// Reads only what is loaded on this machine.</summary>
    internal static class SeaGatePairRules
    {
        private const float MaxHeightDifference = 4f;
        private const float SearchWidths = 2f;
        private const float WidthTolerance = 0.05f;

        // Measured spots when the caller doesn't want them.
        private static readonly List<DepthSample> scratch = new List<DepthSample>();

        /// <summary>The verdict for a pillar at <paramref name="position"/>. When the water was measured, the nine spots
        /// go into <paramref name="samples"/> (cleared first; may be null) for the preview to draw.</summary>
        public static PairCheck Check(Vector3 position, long selfId, List<DepthSample> samples)
        {
            samples?.Clear();
            SeaGatePillar candidate = Nearest(position, selfId);
            float depth = SeaGateFooting.DepthOf(position.y);
            if (depth > SeaGateFooting.MaxDepth)
                return Fail(candidate, SeaGateWords.PairTooDeep, depth, SeaGateFooting.MaxDepth);
            if (candidate == null)
                return Fail(null, SeaGateWords.Unpaired);
            PairCheck placed = PlaceCheck(position, candidate);
            if (!placed.Ok)
                return placed;
            List<DepthSample> spots = samples ?? scratch;
            SeaGateDepth.Survey(FrameFor(position, selfId, candidate), WayfareConfig.MinWaterDepth.Value, spots);
            return WaterCheck(candidate, spots);
        }

        private static PairCheck WaterCheck(SeaGatePillar candidate, List<DepthSample> spots)
        {
            float minDepth = WayfareConfig.MinWaterDepth.Value;
            string problem = SeaGateDepth.SpanProblem(spots, out float shallowest);
            if (problem != null)
                return Fail(candidate, problem, Mathf.Max(0f, shallowest), minDepth);
            int sides = SeaGateDepth.UsableSides(spots, out float sideShallowest);
            if (sides == 0)
                return Fail(candidate, SeaGateWords.PairShallow, Mathf.Max(0f, sideShallowest), minDepth);
            return new PairCheck(candidate, true, sides, SeaGateWords.PairOk);
        }

        private static PairCheck Fail(SeaGatePillar candidate, string reasonToken, float value = 0f, float limit = 0f) =>
            new PairCheck(candidate, false, 0, reasonToken, value, limit);

        /// <summary>Width and height against the candidate; Ok (with no reason) when both pass.</summary>
        private static PairCheck PlaceCheck(Vector3 position, SeaGatePillar candidate)
        {
            Vector3 other = candidate.transform.position;
            float distance = FlatDistance(position, other);
            if (distance < WayfareConfig.MinGateWidth.Value)
                return Fail(candidate, SeaGateWords.PairTooClose, distance, WayfareConfig.MinGateWidth.Value);
            if (distance > WayfareConfig.MaxGateWidth.Value)
                return Fail(candidate, SeaGateWords.PairTooFar, distance, WayfareConfig.MaxGateWidth.Value);
            float rise = Mathf.Abs(position.y - other.y);
            if (rise > MaxHeightDifference)
                return Fail(candidate, SeaGateWords.PairHeight, rise, MaxHeightDifference);
            return new PairCheck(candidate, true, 0, null);
        }

        private static bool Placeable(Vector3 position, Vector3 other)
        {
            float distance = FlatDistance(position, other);
            return distance >= WayfareConfig.MinGateWidth.Value && distance <= WayfareConfig.MaxGateWidth.Value &&
                   Mathf.Abs(position.y - other.y) <= MaxHeightDifference;
        }

        /// <summary>The gate's frame anchor first (the smaller id), so the sides mean the same on both pillars. Without
        /// an id of its own (the preview) the position stands in as the anchor; only the verdict matters there.</summary>
        private static GateGeometry FrameFor(Vector3 position, long selfId, SeaGatePillar candidate)
        {
            Vector3 other = candidate.transform.position;
            bool selfIsAnchor = selfId == 0L || selfId < candidate.Id;
            return selfIsAnchor ? new GateGeometry(position, other) : new GateGeometry(other, position);
        }

        /// <summary>The pillar to pair with: the nearest free pillar at a width and height the rules allow, so a stray
        /// pillar standing too close never hides a good one; else, for the preview's reason, the nearest free pillar
        /// within twice the widest gate.</summary>
        private static SeaGatePillar Nearest(Vector3 position, long selfId)
        {
            SeaGatePillar placeable = Nearest(position, selfId, true);
            return placeable != null ? placeable : Nearest(position, selfId, false);
        }

        /// <summary>The nearest free pillar within twice the widest gate, by distance along the ground; with
        /// <paramref name="placeableOnly"/>, only those that pass the width and height rules.</summary>
        private static SeaGatePillar Nearest(Vector3 position, long selfId, bool placeableOnly)
        {
            float best = WayfareConfig.MaxGateWidth.Value * SearchWidths;
            SeaGatePillar nearest = null;
            IReadOnlyList<SeaGatePillar> pillars = SeaGateRegistry.Pillars;
            for (int i = 0; i < pillars.Count; i++)
            {
                SeaGatePillar pillar = pillars[i];
                if (!IsCandidate(pillar, selfId) || (placeableOnly && !Placeable(position, pillar.transform.position)))
                    continue;
                float distance = FlatDistance(position, pillar.transform.position);
                if (distance <= best)
                {
                    best = distance;
                    nearest = pillar;
                }
            }
            return nearest;
        }

        private static bool IsCandidate(SeaGatePillar pillar, long selfId)
        {
            if (pillar == null || !pillar.IsValid)
                return false;
            long id = pillar.Id;
            return id != 0L && id != selfId && !HasLivePartner(pillar);
        }

        /// <summary>Whether a pillar is in a gate right now: its partner is loaded here and names it back.</summary>
        public static bool HasLivePartner(SeaGatePillar pillar)
        {
            SeaGatePillar partner = SeaGateRegistry.FindLoaded(SeaGateFields.GetPartner(pillar.Zdo));
            return partner != null && SeaGateFields.IsMutual(pillar.Zdo, partner.Zdo);
        }

        /// <summary>Whether two pillars stand within the gate width range (a hair of tolerance for float noise).</summary>
        public static bool InWidth(Vector3 a, Vector3 b)
        {
            float distance = FlatDistance(a, b);
            return distance >= WayfareConfig.MinGateWidth.Value - WidthTolerance && distance <= WayfareConfig.MaxGateWidth.Value + WidthTolerance;
        }

        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
