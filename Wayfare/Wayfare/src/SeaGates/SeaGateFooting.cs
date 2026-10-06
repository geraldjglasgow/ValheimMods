using System.Globalization;
using HarmonyLib;
using UnityEngine;
using Wayfare.Core;

namespace Wayfare.SeaGates
{
    /// <summary>A sea gate pillar stands on the ground: on land, or on the bottom of water at most
    /// <see cref="MaxDepth"/> deep, so building at the waterline is easy and the pillar's top (about 4.5 m up) always
    /// stays above the sea as a dry spot for the crew's fallback landing. The game's placement ray passes through water
    /// for this piece (it is neither a water piece nor kept out of water), so the ghost sits on the seabed. While the
    /// local player holds a pillar ghost, placement is invalid when the ghost's base is deeper than that. The game sets
    /// <c>Player.m_placementStatus</c> and the ghost's colour at the end of <c>Player.UpdatePlacementGhost</c>
    /// (<c>SetPlacementGhostValid</c>); the postfix overrides both, and <c>Player.TryPlacePiece</c>, which calls it and
    /// then reads the status, refuses the pillar. The base is the lowest point of the ghost's visible meshes, which the
    /// game sets down on the ray hit. Run from the one placement ghost patch (<see cref="PillarGhostPatch"/>).</summary>
    public static class SeaGateFooting
    {
        /// <summary>The deepest water a pillar's base may stand in, in metres below sea level.</summary>
        public const float MaxDepth = 2f;

        private static GameObject cachedGhost;
        private static Renderer[] cachedRenderers;
        private static float refusedDepth;

        /// <summary>Whether this check made the current placement invalid, so the refusal can say why.</summary>
        public static bool MadeInvalid { get; private set; }

        private static bool On => WayfareConfig.Enabled.Value && WayfareConfig.SeaGatesEnabled.Value;

        /// <summary>How far below sea level a base at this height stands (zero or less on land).</summary>
        public static float DepthOf(float baseHeight) => SeaGateFields.WaterLevel - baseHeight;

        /// <summary>The reason text for a base standing <paramref name="depth"/> metres under water.</summary>
        public static string Describe(float depth)
        {
            string text = Localization.instance != null ? Localization.instance.Localize(SeaGateWords.PairTooDeep) : SeaGateWords.PairTooDeep;
            return string.Format(text, Metres(depth), Metres(MaxDepth));
        }

        internal static void Check(Player player)
        {
            MadeInvalid = false;
            if (!On || player == null || player != Player.m_localPlayer || ZoneSystem.instance == null)
                return;
            GameObject ghost = player.m_placementGhost;
            if (ghost == null || !ghost.activeSelf || player.m_placementStatus != Player.PlacementStatus.Valid)
                return;
            if (!SeaGatePreview.IsPillarGhost(ghost))
                return;
            float depth = DepthOf(BaseHeight(ghost));
            if (depth <= MaxDepth)
                return;
            player.m_placementStatus = Player.PlacementStatus.Invalid;
            player.SetPlacementGhostValid(false);
            refusedDepth = depth;
            MadeInvalid = true;
        }

        /// <summary>After a refused placement this check caused: say why instead of the game's "invalid placement".</summary>
        internal static void Explain(Player player)
        {
            if (MadeInvalid && player != null && player == Player.m_localPlayer)
                player.Message(MessageHud.MessageType.Center, Describe(refusedDepth));
        }

        private static float BaseHeight(GameObject ghost)
        {
            if (ghost != cachedGhost)
            {
                cachedGhost = ghost;
                cachedRenderers = ghost.GetComponentsInChildren<MeshRenderer>(true);
            }
            float lowest = float.MaxValue;
            foreach (Renderer renderer in cachedRenderers)
            {
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    lowest = Mathf.Min(lowest, renderer.bounds.min.y);
            }
            return lowest < float.MaxValue ? lowest : ghost.transform.position.y;
        }

        private static string Metres(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class SeaGateFootingPlacePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, bool __result)
        {
            if (!__result)
                SeaGateFooting.Explain(__instance);
        }
    }
}
